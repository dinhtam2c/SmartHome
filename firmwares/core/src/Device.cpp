#include "Device.h"

#include <Arduino.h>
#include <ArduinoJson.h>

#include <cctype>
#include <unordered_map>
#include <unordered_set>

namespace {
    std::string toLowerAscii(const std::string& value) {
        std::string normalized;
        normalized.reserve(value.size());
        for (char ch : value) {
            normalized.push_back(static_cast<char>(std::tolower(static_cast<unsigned char>(ch))));
        }
        return normalized;
    }

    std::string capabilityStateCacheKey(const CapabilityDefinition& capability) {
        return capability.capabilityId + "|" + capability.endpointId;
    }

    bool supportsOperation(const CapabilityDefinition& capability, const std::string& operation) {
        if (operation.empty()) {
            return false;
        }

        for (const auto& supported : capability.supportedOperations) {
            if (supported.size() == operation.size()) {
                bool matches = true;
                for (size_t i = 0; i < operation.size(); ++i) {
                    if (std::tolower(static_cast<unsigned char>(supported[i])) !=
                        std::tolower(static_cast<unsigned char>(operation[i]))) {
                        matches = false;
                        break;
                    }
                }

                if (matches) {
                    return true;
                }
            }
        }

        return false;
    }
}

void Device::begin() {
    Serial.begin(115200);

    if (!validateEndpointContracts() || !validateCapabilityContracts()) {
        Serial.println("[Device] capability contract validation failed. MQTT startup aborted.");
        return;
    }

    _configuration.commitDefaults();
    _wifiManager.begin(_configuration);
    _provisioningManager.begin(_configuration, _wifiManager, _apSsid, _apPassword,
        _deviceName, _firmwareVersion, _endpoints, _capabilities);
    _provisioningManager.waitForProvisioning();
    setupMqtt();
}

void Device::setupMqtt() {
    ServerPreference serverPreference = _configuration.server();
    CredentialPreference credentialPreference = _configuration.credentials();

    if (serverPreference.address.empty() || serverPreference.port == 0) {
        Serial.println("Server configuration missing. Skip MQTT setup.");
        return;
    }

    std::string offlinePayload = "{\"state\":\"Offline\"}";
    Will will{ topicAvailability(), offlinePayload, 1, true };

    attachMqttHandlers();

    _mqttManager.begin(
        &_wifiManager,
        serverPreference.address,
        serverPreference.port,
        credentialPreference.deviceId,
        false,
        &will,
        credentialPreference.deviceId,
        credentialPreference.token);
}

void Device::loop() {
    if (!_mqttManager.isConnected()) {
        return;
    }

    unsigned long now = millis();
    if (now - _lastAvailabilitySentAt >= AVAILABILITY_SEND_INTERVAL_MS) {
        publishAvailability("Online", true);
        _lastAvailabilitySentAt = now;
    }

    if (now - _lastSystemSentAt >= SYSTEM_STATE_SEND_INTERVAL_MS) {
        publishSystemState();
        publishCapabilityStates(false);
        _lastSystemSentAt = now;
    }
}

void Device::attachMqttHandlers() {
    _mqttManager.onConnected([this]() {
        _mqttManager.subscribe(topicCommand(), 2);
        publishAvailability("Online", true);
        publishSystemState();
        publishCapabilityStates(true);
        _lastAvailabilitySentAt = millis();
        _lastSystemSentAt = millis();
        });

    _mqttManager.onDisconnected([this](AsyncMqttClientDisconnectReason) {
        Serial.println("[Device] runtime MQTT disconnected");
        });

    _mqttManager.onMessage([this](const std::string& topic, const std::string& payload) {
        if (topic == topicCommand()) {
            handleCommandMessage(payload);
        }
        });
}

std::string Device::deviceId() const {
    return _configuration.credentials().deviceId;
}

std::string Device::topicAvailability() const {
    return std::string("home/devices/") + deviceId() + "/availability";
}

std::string Device::topicSystemState() const {
    return std::string("home/devices/") + deviceId() + "/states/system";
}

std::string Device::topicCapabilityState() const {
    return std::string("home/devices/") + deviceId() + "/states/capabilities";
}

std::string Device::topicCommand() const {
    return std::string("home/devices/") + deviceId() + "/command";
}

std::string Device::topicCommandResult() const {
    return std::string("home/devices/") + deviceId() + "/command/result";
}

void Device::publishAvailability(const char* state, bool retain) {
    JsonDocument payload;
    payload["state"] = state;

    std::string body;
    serializeJson(payload, body);
    _mqttManager.publish(topicAvailability(), body, 0, retain);
}

void Device::publishSystemState() {
    JsonDocument payload;
    payload["uptime"] = static_cast<uint32_t>(millis() / 1000UL);

    std::string body;
    serializeJson(payload, body);
    _mqttManager.publish(topicSystemState(), body, 0, false);
}

void Device::publishCapabilityStates(bool forceAll) {
    JsonDocument payload;
    JsonArray deltas = payload.to<JsonArray>();

    for (const auto& capability : _capabilities) {
        if (!capability.getStateJson) {
            continue;
        }

        std::string stateJson = capability.getStateJson();
        std::string cacheKey = capabilityStateCacheKey(capability);
        if (!forceAll) {
            auto found = _lastCapabilityState.find(cacheKey);
            if (found != _lastCapabilityState.end() && found->second == stateJson) {
                continue;
            }
        }

        JsonDocument stateDoc;
        if (deserializeJson(stateDoc, stateJson.c_str()) != DeserializationError::Ok ||
            !stateDoc.is<JsonObject>()) {
            continue;
        }

        JsonObject delta = deltas.add<JsonObject>();
        delta["capabilityId"] = capability.capabilityId.c_str();
        delta["endpointId"] = capability.endpointId.c_str();
        JsonObject state = delta["state"].to<JsonObject>();
        state.set(stateDoc.as<JsonObjectConst>());

        _lastCapabilityState[cacheKey] = stateJson;
    }

    if (deltas.size() == 0) {
        return;
    }

    std::string body;
    serializeJson(payload, body);
    _mqttManager.publish(topicCapabilityState(), body, 1, false);
}

const EndpointDefinition* Device::findEndpoint(const std::string& endpointId) const {
    std::string normalizedEndpointId = toLowerAscii(endpointId);
    for (const auto& endpoint : _endpoints) {
        if (toLowerAscii(endpoint.endpointId) == normalizedEndpointId) {
            return &endpoint;
        }
    }

    return nullptr;
}

const CapabilityDefinition* Device::findCapability(const std::string& capabilityId,
    const std::string& endpointId) const {
    std::string normalizedCapabilityId = toLowerAscii(capabilityId);
    std::string normalizedEndpointId = toLowerAscii(endpointId);
    for (const auto& capability : _capabilities) {
        if (toLowerAscii(capability.capabilityId) != normalizedCapabilityId ||
            toLowerAscii(capability.endpointId) != normalizedEndpointId) {
            continue;
        }

        return &capability;
    }

    return nullptr;
}

void Device::handleCommandMessage(const std::string& payload) {
    JsonDocument doc;
    if (deserializeJson(doc, payload.c_str()) != DeserializationError::Ok) {
        return;
    }

    std::string capabilityId = doc["capabilityId"] | "";
    std::string endpointId = doc["endpointId"] | "";
    std::string operation = doc["operation"] | "";
    std::string correlationId = doc["correlationId"] | "";

    if (correlationId.empty()) {
        return;
    }

    if (capabilityId.empty()) {
        publishCommandResult("", correlationId, operation, "Failed", "", "capability_required");
        return;
    }

    if (endpointId.empty()) {
        publishCommandResult(capabilityId, correlationId, operation, "Failed", "", "endpoint_required");
        return;
    }

    if (operation.empty()) {
        publishCommandResult(capabilityId, correlationId, operation, "Failed", "", "operation_required");
        return;
    }

    const CapabilityDefinition* capability = findCapability(capabilityId, endpointId);
    if (capability == nullptr) {
        publishCommandResult(capabilityId, correlationId, operation, "Failed", "", "capability_not_found");
        return;
    }

    if (!capability->supportedOperations.empty() && !supportsOperation(*capability, operation)) {
        publishCommandResult(capabilityId, correlationId, operation, "Failed", "", "operation_not_supported");
        return;
    }

    if (!capability->handleCommand) {
        publishCommandResult(capabilityId, correlationId, operation, "Failed", "", "operation_not_supported");
        return;
    }

    std::string outStateJson;
    std::string outError;
    bool handled = capability->handleCommand(operation, doc["value"], outStateJson, outError);
    if (!handled) {
        publishCommandResult(capabilityId, correlationId, operation, "Failed", "", "operation_not_supported");
        return;
    }

    if (!outError.empty()) {
        publishCommandResult(capabilityId, correlationId, operation, "Failed", "", outError);
        return;
    }

    publishCommandResult(capabilityId, correlationId, operation, "Completed", outStateJson, "");
    publishCapabilityStates(false);
}

void Device::publishCommandResult(const std::string& capabilityId,
    const std::string& correlationId,
    const std::string& operation,
    const std::string& status,
    const std::string& valueJson,
    const std::string& error) {
    JsonDocument payload;
    payload["capabilityId"] = capabilityId.c_str();
    payload["correlationId"] = correlationId.c_str();
    payload["operation"] = operation.c_str();
    payload["status"] = status.c_str();

    if (!valueJson.empty()) {
        JsonDocument valueDoc;
        if (deserializeJson(valueDoc, valueJson.c_str()) == DeserializationError::Ok) {
            payload["value"].set(valueDoc.as<JsonVariantConst>());
        } else {
            payload["value"] = nullptr;
        }
    } else {
        payload["value"] = nullptr;
    }

    if (!error.empty()) {
        payload["error"] = error.c_str();
    } else {
        payload["error"] = nullptr;
    }

    std::string body;
    serializeJson(payload, body);
    _mqttManager.publish(topicCommandResult(), body, 1, false);
}

bool Device::validateEndpointContracts() const {
    if (_endpoints.empty()) {
        Serial.println("[Device] invalid endpoint contract: at least one endpoint is required");
        return false;
    }

    std::unordered_set<std::string> endpointCodes;
    for (const auto& endpoint : _endpoints) {
        if (endpoint.endpointId.empty()) {
            Serial.println("[Device] invalid endpoint contract: endpoint.endpointId cannot be empty");
            return false;
        }

        std::string normalizedCode = toLowerAscii(endpoint.endpointId);
        auto inserted = endpointCodes.insert(normalizedCode);
        if (!inserted.second) {
            Serial.printf("[Device] invalid endpoint contract: duplicate endpoint.endpointId=%s\r\n",
                endpoint.endpointId.c_str());
            return false;
        }
    }

    return true;
}

bool Device::validateCapabilityContracts() const {
    if (_capabilities.empty()) {
        Serial.println("[Device] invalid capability contract: at least one capability is required");
        return false;
    }

    std::unordered_set<std::string> pairSeen;
    std::unordered_map<std::string, size_t> capabilityCountByEndpoint;

    for (const auto& capability : _capabilities) {
        if (capability.capabilityId.empty()) {
            Serial.println("[Device] invalid capability contract: capabilityId cannot be empty");
            return false;
        }

        bool hasStateReporter = static_cast<bool>(capability.getStateJson);
        bool hasCommandHandler = static_cast<bool>(capability.handleCommand);
        if (!hasStateReporter && !hasCommandHandler) {
            Serial.printf("[Device] invalid capability contract: capability must expose state or command handler capabilityId=%s endpointId=%s\r\n",
                capability.capabilityId.c_str(), capability.endpointId.c_str());
            return false;
        }

        if (capability.capabilityVersion <= 0) {
            Serial.printf("[Device] invalid capability contract: capabilityVersion must be positive for capabilityId=%s\r\n",
                capability.capabilityId.c_str());
            return false;
        }

        if (capability.endpointId.empty()) {
            Serial.printf("[Device] invalid capability contract: endpointId is required for capabilityId=%s\r\n",
                capability.capabilityId.c_str());
            return false;
        }

        if (findEndpoint(capability.endpointId) == nullptr) {
            Serial.printf("[Device] invalid capability contract: endpointId=%s is not declared in endpoints list\r\n",
                capability.endpointId.c_str());
            return false;
        }

        if (!capability.handleCommand && !capability.supportedOperations.empty()) {
            Serial.printf("[Device] invalid capability contract: supportedOperations declared without handler capabilityId=%s endpointId=%s\r\n",
                capability.capabilityId.c_str(), capability.endpointId.c_str());
            return false;
        }

        if (capability.handleCommand && capability.supportedOperations.empty()) {
            Serial.printf("[Device] invalid capability contract: command handler requires supportedOperations capabilityId=%s endpointId=%s\r\n",
                capability.capabilityId.c_str(), capability.endpointId.c_str());
            return false;
        }

        std::unordered_set<std::string> operationsSeen;
        for (const auto& operation : capability.supportedOperations) {
            if (operation.empty()) {
                Serial.printf("[Device] invalid capability contract: empty supportedOperations item capabilityId=%s endpointId=%s\r\n",
                    capability.capabilityId.c_str(), capability.endpointId.c_str());
                return false;
            }

            std::string normalizedOperation = toLowerAscii(operation);
            auto operationInserted = operationsSeen.insert(normalizedOperation);
            if (!operationInserted.second) {
                Serial.printf("[Device] invalid capability contract: duplicate supportedOperations item capabilityId=%s endpointId=%s operation=%s\r\n",
                    capability.capabilityId.c_str(), capability.endpointId.c_str(), operation.c_str());
                return false;
            }
        }

        std::string pairKey = toLowerAscii(capability.endpointId) + "|" + toLowerAscii(capability.capabilityId);
        auto inserted = pairSeen.insert(pairKey);
        if (!inserted.second) {
            Serial.printf("[Device] invalid capability contract: duplicate capabilityId+endpointId pair capabilityId=%s endpointId=%s\r\n",
                capability.capabilityId.c_str(), capability.endpointId.c_str());
            return false;
        }

        capabilityCountByEndpoint[toLowerAscii(capability.endpointId)]++;
    }

    for (const auto& endpoint : _endpoints) {
        std::string key = toLowerAscii(endpoint.endpointId);
        auto found = capabilityCountByEndpoint.find(key);
        if (found == capabilityCountByEndpoint.end() || found->second == 0) {
            Serial.printf("[Device] invalid capability contract: endpointId=%s has no capabilities\r\n",
                endpoint.endpointId.c_str());
            return false;
        }
    }

    return true;
}

void Device::resetConfiguration() {
    _configuration.reset();
}
