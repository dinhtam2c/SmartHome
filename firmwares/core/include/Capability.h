#pragma once

#include <ArduinoJson.h>

#include <functional>
#include <string>
#include <vector>

struct EndpointDefinition {
    std::string endpointId;
    std::string name;
};

struct CapabilityDefinition {
    std::string capabilityId;
    int capabilityVersion = 1;
    std::vector<std::string> supportedOperations;
    std::string endpointId;

    // Must return a JSON object string (for example: {"value":true}).
    std::function<std::string()> getStateJson;

    // Returns true if command is handled; false means unsupported operation.
    // commandValue is the `value` field from DeviceCommand payload.
    // outStateJson should be set to a JSON object string for successful updates.
    std::function<bool(const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateJson, std::string& outError)> handleCommand;
};
