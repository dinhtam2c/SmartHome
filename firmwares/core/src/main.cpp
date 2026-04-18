#include <Arduino.h>

#include "Capability.h"
#include "Device.h"

std::string apSsid = "SmartHome-Setup";
std::string apPassword = "toidepchai";
std::string deviceName = "SmartHome-Core";
std::string firmwareVersion = "1.0.0";

namespace {
    int gLightLevel = 0;

    EndpointDefinition buildMainEndpoint() {
        EndpointDefinition endpoint;
        endpoint.endpointId = "main";
        endpoint.name = "Main Endpoint";
        return endpoint;
    }

    CapabilityDefinition buildLightDimmerCapability() {
        CapabilityDefinition capability;
        capability.capabilityId = "light.dimmer";
        capability.capabilityVersion = 1;
        capability.supportedOperations = { "setLevel" };
        capability.endpointId = "main";

        capability.getStateJson = []() {
            JsonDocument doc;
            doc["level"] = gLightLevel;
            std::string state;
            serializeJson(doc, state);
            return state;
            };

        capability.handleCommand = [](const std::string& operation, JsonVariantConst commandValue,
            std::string& outStateJson, std::string& outError) {
                if (operation != "setLevel") {
                    outError = "operation_not_supported";
                    return false;
                }

                if (!commandValue.is<JsonObjectConst>()) {
                    outError = "invalid_value";
                    return true;
                }

                JsonObjectConst payload = commandValue.as<JsonObjectConst>();
                JsonVariantConst levelVariant = payload["level"];
                if (levelVariant.isNull() || !levelVariant.is<int>()) {
                    outError = "invalid_value";
                    return true;
                }

                int level = levelVariant.as<int>();
                if (level < 0 || level > 100) {
                    outError = "invalid_value";
                    return true;
                }

                gLightLevel = level;

                JsonDocument state;
                state["level"] = gLightLevel;
                serializeJson(state, outStateJson);

                if (gLightLevel > 0) {
                    Serial.printf("Light level set to %d\r\n", gLightLevel);
                    digitalWrite(LED_BUILTIN, HIGH);
                } else {
                    Serial.println("Light level set to 0");
                    digitalWrite(LED_BUILTIN, LOW);
                }
                return true;
            };

        return capability;
    }
}

std::vector<EndpointDefinition> endpoints = {
    buildMainEndpoint(),
};

std::vector<CapabilityDefinition> capabilities = {
    buildLightDimmerCapability(),
};

Device device(apSsid, apPassword, deviceName, firmwareVersion, endpoints, capabilities);

void setup() {
    pinMode(LED_BUILTIN, OUTPUT);
    device.begin();
}

void loop() {
    device.loop();
}
