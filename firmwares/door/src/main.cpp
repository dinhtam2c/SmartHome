#include <Arduino.h>
#include "Device.h"

namespace {
    constexpr uint8_t RESET_PIN = 0;
    constexpr uint8_t SG90_PIN = 12;
    constexpr uint8_t BUZZER_PIN = 26;

    constexpr uint8_t SG90_PWM_CHANNEL = 0;
    constexpr uint32_t SG90_PWM_FREQUENCY_HZ = 50;
    constexpr uint8_t SG90_PWM_RESOLUTION_BITS = 16;
    constexpr uint32_t SG90_PWM_MAX_DUTY = (1UL << SG90_PWM_RESOLUTION_BITS) - 1;
    constexpr uint32_t SG90_PWM_PERIOD_US = 1000000UL / SG90_PWM_FREQUENCY_HZ;

    constexpr int SG90_MIN_PULSE_WIDTH_US = 500;
    constexpr int SG90_MAX_PULSE_WIDTH_US = 2400;
    constexpr int SG90_MIN_ANGLE_DEGREES = 0;
    constexpr int SG90_MAX_ANGLE_DEGREES = 180;

    constexpr int SG90_LOCKED_ANGLE_DEGREES = 0;
    constexpr int SG90_UNLOCKED_ANGLE_DEGREES = 90;

    constexpr unsigned long RESET_HOLD_MS = 5000;
}

std::string apSsid = "Door-Setup";
std::string apPassword = "toidepchai";
std::string deviceName = "Door";
std::string deviceCategory = "door";
std::string firmwareVersion = "1.0.0";

bool isLocked = true;
bool buzzerActive = false;
unsigned long buzzerUntilMs = 0;

EndpointDefinition mainEndpoint = { "main", "Door" };
std::vector<EndpointDefinition> endpoints = { mainEndpoint };

std::string lockStateJson() {
    return "{\"locked\":" + std::string(isLocked ? "true" : "false") + "}";
}

uint32_t pulseWidthToDuty(int pulseWidthUs) {
    uint32_t boundedPulseWidth = constrain(
        pulseWidthUs,
        SG90_MIN_PULSE_WIDTH_US,
        SG90_MAX_PULSE_WIDTH_US);

    return (boundedPulseWidth * SG90_PWM_MAX_DUTY + SG90_PWM_PERIOD_US / 2) /
        SG90_PWM_PERIOD_US;
}

int angleToPulseWidth(int angleDegrees) {
    int boundedAngle = constrain(
        angleDegrees,
        SG90_MIN_ANGLE_DEGREES,
        SG90_MAX_ANGLE_DEGREES);

    return map(
        boundedAngle,
        SG90_MIN_ANGLE_DEGREES,
        SG90_MAX_ANGLE_DEGREES,
        SG90_MIN_PULSE_WIDTH_US,
        SG90_MAX_PULSE_WIDTH_US);
}

void writeSg90Angle(int angleDegrees) {
    ledcWrite(SG90_PWM_CHANNEL, pulseWidthToDuty(angleToPulseWidth(angleDegrees)));
}

void applyLockStateToServo() {
    int angle = isLocked ? SG90_LOCKED_ANGLE_DEGREES : SG90_UNLOCKED_ANGLE_DEGREES;
    writeSg90Angle(angle);

    Serial.print("[Door] Lock state ");
    Serial.print(isLocked ? "locked" : "unlocked");
    Serial.print(", SG90 angle=");
    Serial.println(angle);
}

void setLockState(bool locked) {
    if (isLocked == locked) {
        applyLockStateToServo();
        return;
    }

    isLocked = locked;
    applyLockStateToServo();
}

bool readLockCommandValue(JsonVariantConst commandValue, bool& locked) {
    if (commandValue.is<bool>()) {
        locked = commandValue.as<bool>();
        return true;
    }

    JsonVariantConst value = commandValue["locked"];
    if (value.is<bool>()) {
        locked = value.as<bool>();
        return true;
    }

    return false;
}

void setBuzzerActive(bool active) {
    if (buzzerActive == active) {
        return;
    }

    buzzerActive = active;
    digitalWrite(BUZZER_PIN, buzzerActive ? HIGH : LOW);
}

void startBuzzer(unsigned long durationMs) {
    buzzerUntilMs = millis() + durationMs;
    setBuzzerActive(true);

    Serial.print("[Door] Buzzer beep durationMs=");
    Serial.println(durationMs);
}

void updateBuzzer(unsigned long now) {
    if (!buzzerActive || static_cast<long>(now - buzzerUntilMs) < 0) {
        return;
    }

    setBuzzerActive(false);
}

bool readBuzzerCommandValue(JsonVariantConst commandValue, unsigned long& durationMs) {
    if (!commandValue.is<JsonObjectConst>() || !commandValue["durationMs"].is<int>()) {
        return false;
    }

    int candidate = commandValue["durationMs"].as<int>();
    if (candidate < 10 || candidate > 10000) {
        return false;
    }

    durationMs = static_cast<unsigned long>(candidate);
    return true;
}

CapabilityDefinition buildLockStateCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "lock.state";
    capability.capabilityVersion = 1;
    capability.endpointId = mainEndpoint.endpointId;
    capability.supportedOperations = { "set" };
    capability.getStateJson = []() {
        return lockStateJson();
        };

    capability.handleCommand = [](
        const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateJson, std::string& outError) {
            if (operation != "set") {
                outError = "operation_not_supported";
                return false;
            }

            bool locked = isLocked;
            if (!readLockCommandValue(commandValue, locked)) {
                outError = "invalid_value";
                return true;
            }

            setLockState(locked);
            outStateJson = lockStateJson();
            return true;
        };

    return capability;
}

CapabilityDefinition buildBuzzerCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "buzzer";
    capability.capabilityVersion = 1;
    capability.endpointId = mainEndpoint.endpointId;
    capability.supportedOperations = { "beep" };
    capability.getStateJson = nullptr;

    capability.handleCommand = [](
        const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateJson, std::string& outError) {
            if (operation != "beep") {
                outError = "operation_not_supported";
                return false;
            }

            unsigned long durationMs = 0;
            if (!readBuzzerCommandValue(commandValue, durationMs)) {
                outError = "invalid_value";
                return true;
            }

            startBuzzer(durationMs);
            return true;
        };

    return capability;
}

std::vector<CapabilityDefinition> capabilities = {
    buildLockStateCapability(),
    buildBuzzerCapability()
};

Device device(apSsid, apPassword, deviceName, deviceCategory, firmwareVersion, endpoints, capabilities);

void setup() {
    Serial.begin(115200);
    pinMode(RESET_PIN, INPUT_PULLUP);
    pinMode(BUZZER_PIN, OUTPUT);
    digitalWrite(BUZZER_PIN, LOW);

    ledcSetup(SG90_PWM_CHANNEL, SG90_PWM_FREQUENCY_HZ, SG90_PWM_RESOLUTION_BITS);
    ledcAttachPin(SG90_PIN, SG90_PWM_CHANNEL);
    applyLockStateToServo();

    device.begin();
}

unsigned long resetPressStart = 0;
bool wasResetPressed = false;

void loop() {
    unsigned long now = millis();

    updateBuzzer(now);
    device.publishCapabilityStates();

    if (digitalRead(RESET_PIN) == LOW) {
        if (!wasResetPressed) {
            resetPressStart = now;
            wasResetPressed = true;
        } else if (now - resetPressStart >= RESET_HOLD_MS) {
            device.resetConfiguration();
            ESP.restart();
        }
    } else {
        wasResetPressed = false;
    }

    device.loop();
}
