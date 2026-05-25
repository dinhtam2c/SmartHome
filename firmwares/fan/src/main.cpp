#include <Arduino.h>

#include "Capability.h"
#include "Device.h"

namespace {
    constexpr uint8_t RESET_PIN = 0;
    constexpr uint8_t FAN_PIN = 14;
    constexpr uint8_t FAN_PWM_CHANNEL = 0;
    constexpr uint32_t FAN_PWM_FREQUENCY_HZ = 25000;
    constexpr uint8_t FAN_PWM_RESOLUTION_BITS = 8;
    constexpr int FAN_PWM_MAX_DUTY = 255;
    constexpr unsigned long RESET_HOLD_MS = 5000;
}

std::string apSsid = "Fan-Setup";
std::string apPassword = "toidepchai";
std::string deviceName = "Fan";
std::string deviceCategory = "fan";
std::string firmwareVersion = "1.0.0";

bool fanPower = false;

EndpointDefinition mainEndpoint = { "main", "Fan" };

std::string boolStateJson(bool value) {
    return value ? "{\"value\":true}" : "{\"value\":false}";
}

CapabilityDefinition buildPowerCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "switch.power";
    capability.capabilityVersion = 1;
    capability.supportedOperations = { "set" };
    capability.endpointId = "main";

    capability.getStateJson = []() {
        return boolStateJson(fanPower);
        };

    capability.handleCommand = [](const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateJson, std::string& outError) {
            if (operation != "set") {
                outError = "operation_not_supported";
                return false;
            }

            if (!commandValue["value"].is<bool>()) {
                outError = "invalid_value";
                return true;
            }

            fanPower = commandValue["value"].as<bool>();
            digitalWrite(FAN_PIN, fanPower ? HIGH : LOW);

            outStateJson = boolStateJson(fanPower);
            return true;
        };

    return capability;
}

std::vector<EndpointDefinition> endpoints = { mainEndpoint };
std::vector<CapabilityDefinition> capabilities = {
    buildPowerCapability()
};

Device device(apSsid, apPassword, deviceName, deviceCategory, firmwareVersion, endpoints, capabilities);

void setup() {
    Serial.begin(115200);

    pinMode(RESET_PIN, INPUT_PULLUP);
    pinMode(FAN_PIN, OUTPUT);

    device.begin();
}

unsigned long resetPressStart = 0;
bool wasResetPressed = false;

void loop() {
    unsigned long now = millis();

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
