#include <Arduino.h>

#include "Capability.h"
#include "Device.h"

namespace {
    constexpr uint8_t RESET_PIN = 0;
    constexpr uint8_t LED_RED_PIN = 14;
    constexpr uint8_t LED_GREEN_PIN = 12;
    constexpr uint8_t LED_BLUE_PIN = 13;

    constexpr uint8_t LED_RED_CHANNEL = 0;
    constexpr uint8_t LED_GREEN_CHANNEL = 1;
    constexpr uint8_t LED_BLUE_CHANNEL = 2;
    constexpr uint32_t LED_PWM_FREQUENCY_HZ = 5000;
    constexpr uint8_t LED_PWM_RESOLUTION_BITS = 8;
    constexpr int LED_PWM_MAX_DUTY = 255;
    constexpr bool RGB_LED_ACTIVE_HIGH = true;

    constexpr unsigned long RESET_HOLD_MS = 5000;
}

std::string apSsid = "Light-Setup";
std::string apPassword = "toidepchai";
std::string deviceName = "Light";
std::string deviceCategory = "light";
std::string firmwareVersion = "1.0.0";

bool lightPower = false;
int brightnessValue = 100;
int redValue = 255;
int greenValue = 255;
int blueValue = 255;

struct FlashEffectState {
    bool active = false;
    bool visible = false;
    int remainingPhases = 0;
    unsigned long intervalMs = 200;
    unsigned long nextPhaseAt = 0;
    int red = 255;
    int green = 255;
    int blue = 255;
};

FlashEffectState flashEffect;

EndpointDefinition mainEndpoint = { "main", "Light" };

int clampRgb(int value) {
    return constrain(value, 0, LED_PWM_MAX_DUTY);
}

int clampBrightness(int value) {
    return constrain(value, 0, 100);
}

int rgbToDuty(int value) {
    int duty = clampRgb(value);
    return RGB_LED_ACTIVE_HIGH ? duty : LED_PWM_MAX_DUTY - duty;
}

int applyBrightness(int value) {
    return (clampRgb(value) * clampBrightness(brightnessValue) + 50) / 100;
}

std::string boolStateJson(bool value) {
    return value ? "{\"value\":true}" : "{\"value\":false}";
}

std::string brightnessStateJson() {
    return "{\"value\":" + std::to_string(brightnessValue) + "}";
}

std::string rgbStateJson() {
    return "{\"value\":{\"red\":" + std::to_string(redValue) +
        ",\"green\":" + std::to_string(greenValue) +
        ",\"blue\":" + std::to_string(blueValue) + "}}";
}

void logDevice(const char* message) {
    Serial.print("[Device] ");
    Serial.println(message);
}

void logDeviceState(const char* label, bool value) {
    Serial.print("[Device] ");
    Serial.print(label);
    Serial.println(value ? "true" : "false");
}

void logDevicePower(bool power) {
    Serial.print("[Device] Light power ");
    Serial.println(power ? "ON" : "OFF");
}

void logDeviceRgb(int red, int green, int blue) {
    Serial.print("[Device] RGB set to R:");
    Serial.print(red);
    Serial.print(" G:");
    Serial.print(green);
    Serial.print(" B:");
    Serial.println(blue);
}

void writeRgbLed() {
    int outputRed = redValue;
    int outputGreen = greenValue;
    int outputBlue = blueValue;

    if (flashEffect.active) {
        outputRed = flashEffect.visible ? flashEffect.red : 0;
        outputGreen = flashEffect.visible ? flashEffect.green : 0;
        outputBlue = flashEffect.visible ? flashEffect.blue : 0;
    }

    bool shouldOutput = flashEffect.active ? flashEffect.visible : lightPower;
    int redDuty = shouldOutput ? applyBrightness(outputRed) : 0;
    int greenDuty = shouldOutput ? applyBrightness(outputGreen) : 0;
    int blueDuty = shouldOutput ? applyBrightness(outputBlue) : 0;

    ledcWrite(LED_RED_CHANNEL, rgbToDuty(redDuty));
    ledcWrite(LED_GREEN_CHANNEL, rgbToDuty(greenDuty));
    ledcWrite(LED_BLUE_CHANNEL, rgbToDuty(blueDuty));
}

void setLightPower(bool power) {
    if (lightPower == power) {
        return;
    }

    lightPower = power;
    logDevicePower(lightPower);
    writeRgbLed();
}

void setBrightness(int brightness) {
    brightnessValue = clampBrightness(brightness);
    Serial.print("[Device] Brightness set to ");
    Serial.println(brightnessValue);
    writeRgbLed();
}

void setRgbColor(int red, int green, int blue) {
    redValue = clampRgb(red);
    greenValue = clampRgb(green);
    blueValue = clampRgb(blue);
    logDeviceRgb(redValue, greenValue, blueValue);
    writeRgbLed();
}

bool readRgbValue(JsonVariantConst value, int& red, int& green, int& blue) {
    if (!value.is<JsonObjectConst>()) {
        return false;
    }

    if (!value["red"].is<int>() ||
        !value["green"].is<int>() ||
        !value["blue"].is<int>()) {
        return false;
    }

    red = value["red"].as<int>();
    green = value["green"].as<int>();
    blue = value["blue"].as<int>();

    return red >= 0 && red <= LED_PWM_MAX_DUTY &&
        green >= 0 && green <= LED_PWM_MAX_DUTY &&
        blue >= 0 && blue <= LED_PWM_MAX_DUTY;
}

bool readRgbCommandValue(JsonVariantConst commandValue, int& red, int& green, int& blue) {
    return readRgbValue(commandValue["value"], red, green, blue);
}

bool readBrightnessCommandValue(JsonVariantConst commandValue, int& brightness) {
    JsonVariantConst value = commandValue["value"];
    if (!value.is<int>()) {
        return false;
    }

    int candidate = value.as<int>();
    if (candidate < 0 || candidate > 100) {
        return false;
    }

    brightness = candidate;
    return true;
}

bool readFlashEffectCommandValue(
    JsonVariantConst commandValue,
    int& count,
    unsigned long& intervalMs,
    int& red,
    int& green,
    int& blue) {
    if (!commandValue.is<JsonObjectConst>() || !commandValue["count"].is<int>()) {
        return false;
    }

    std::string valueStr;
    serializeJson(commandValue, valueStr);
    Serial.printf("[Device] Flash effect command received: %s\n", valueStr.c_str());

    count = commandValue["count"].as<int>();
    if (count < 1 || count > 10) {
        return false;
    }

    intervalMs = 200;
    if (!commandValue["intervalMs"].isNull()) {
        if (!commandValue["intervalMs"].is<int>()) {
            return false;
        }

        intervalMs = commandValue["intervalMs"].as<unsigned long>();
        if (intervalMs < 20 || intervalMs > 5000) {
            return false;
        }
    }

    red = redValue;
    green = greenValue;
    blue = blueValue;

    JsonVariantConst color = commandValue["color"];
    if (!color.isNull() && !readRgbValue(color, red, green, blue)) {
        return false;
    }

    return true;
}

void startFlashEffect(int count, unsigned long intervalMs, int red, int green, int blue) {
    flashEffect.active = true;
    flashEffect.visible = true;
    flashEffect.remainingPhases = count * 2;
    flashEffect.intervalMs = intervalMs;
    flashEffect.nextPhaseAt = millis() + intervalMs;
    flashEffect.red = clampRgb(red);
    flashEffect.green = clampRgb(green);
    flashEffect.blue = clampRgb(blue);

    writeRgbLed();
}

void updateFlashEffect(unsigned long now) {
    if (!flashEffect.active || static_cast<long>(now - flashEffect.nextPhaseAt) < 0) {
        return;
    }

    flashEffect.remainingPhases--;
    if (flashEffect.remainingPhases <= 0) {
        flashEffect.active = false;
        flashEffect.visible = false;
        writeRgbLed();
        return;
    }

    flashEffect.visible = !flashEffect.visible;
    flashEffect.nextPhaseAt = now + flashEffect.intervalMs;
    writeRgbLed();
}

CapabilityDefinition buildPowerCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "switch.power";
    capability.capabilityVersion = 1;
    capability.supportedOperations = { "set" };
    capability.endpointId = mainEndpoint.endpointId;

    capability.getStateJson = []() {
        return boolStateJson(lightPower);
        };

    capability.handleCommand = [](const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateDeltaJson, std::string& outError) {
            if (operation != "set") {
                outError = "operation_not_supported";
                return false;
            }

            setLightPower(commandValue["value"].as<bool>());
            outStateDeltaJson = boolStateJson(lightPower);
            return true;
        };

    return capability;
}

CapabilityDefinition buildBrightnessCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "light.brightness";
    capability.capabilityVersion = 1;
    capability.supportedOperations = { "set" };
    capability.endpointId = mainEndpoint.endpointId;

    capability.getStateJson = []() {
        return brightnessStateJson();
        };

    capability.handleCommand = [](const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateDeltaJson, std::string& outError) {
            if (operation != "set") {
                outError = "operation_not_supported";
                return false;
            }

            int brightness = brightnessValue;
            if (!readBrightnessCommandValue(commandValue, brightness)) {
                outError = "invalid_value";
                return true;
            }

            setBrightness(brightness);
            outStateDeltaJson = brightnessStateJson();
            return true;
        };

    return capability;
}

CapabilityDefinition buildRgbCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "light.rgb";
    capability.capabilityVersion = 1;
    capability.supportedOperations = { "set" };
    capability.endpointId = mainEndpoint.endpointId;

    capability.getStateJson = []() {
        return rgbStateJson();
        };

    capability.handleCommand = [](const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateDeltaJson, std::string& outError) {
            if (operation != "set") {
                outError = "operation_not_supported";
                return false;
            }

            int red = redValue;
            int green = greenValue;
            int blue = blueValue;
            if (!readRgbCommandValue(commandValue, red, green, blue)) {
                outError = "invalid_value";
                return true;
            }

            setRgbColor(red, green, blue);
            outStateDeltaJson = rgbStateJson();
            return true;
        };

    return capability;
}

CapabilityDefinition buildEffectCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "light.effect";
    capability.capabilityVersion = 1;
    capability.supportedOperations = { "flash" };
    capability.endpointId = mainEndpoint.endpointId;

    capability.getStateJson = nullptr;
    capability.handleCommand = [](const std::string& operation, JsonVariantConst commandValue,
        std::string&, std::string& outError) {
            if (operation != "flash") {
                outError = "operation_not_supported";
                return false;
            }

            int count = 0;
            unsigned long intervalMs = 0;
            int red = redValue;
            int green = greenValue;
            int blue = blueValue;
            if (!readFlashEffectCommandValue(commandValue, count, intervalMs, red, green, blue)) {
                outError = "invalid_value";
                return true;
            }

            startFlashEffect(count, intervalMs, red, green, blue);
            return true;
        };

    return capability;
}

std::vector<EndpointDefinition> endpoints = { mainEndpoint };
std::vector<CapabilityDefinition> capabilities = {
    buildPowerCapability(),
    buildBrightnessCapability(),
    buildRgbCapability(),
    buildEffectCapability(),
};

Device device(apSsid, apPassword, deviceName, deviceCategory, firmwareVersion, endpoints, capabilities);

void setup() {
    Serial.begin(115200);

    pinMode(RESET_PIN, INPUT_PULLUP);

    ledcSetup(LED_RED_CHANNEL, LED_PWM_FREQUENCY_HZ, LED_PWM_RESOLUTION_BITS);
    ledcSetup(LED_GREEN_CHANNEL, LED_PWM_FREQUENCY_HZ, LED_PWM_RESOLUTION_BITS);
    ledcSetup(LED_BLUE_CHANNEL, LED_PWM_FREQUENCY_HZ, LED_PWM_RESOLUTION_BITS);
    ledcAttachPin(LED_RED_PIN, LED_RED_CHANNEL);
    ledcAttachPin(LED_GREEN_PIN, LED_GREEN_CHANNEL);
    ledcAttachPin(LED_BLUE_PIN, LED_BLUE_CHANNEL);
    writeRgbLed();

    device.begin();
}

unsigned long resetPressStart = 0;
bool wasResetPressed = false;

void loop() {
    unsigned long now = millis();

    updateFlashEffect(now);

    if (digitalRead(RESET_PIN) == LOW) {
        if (!wasResetPressed) {
            resetPressStart = now;
            wasResetPressed = true;
            logDevice("Reset button pressed");
        } else if (now - resetPressStart >= RESET_HOLD_MS) {
            logDevice("Resetting configuration");
            device.resetConfiguration();
            ESP.restart();
        }
    } else {
        wasResetPressed = false;
    }

    device.loop();
}
