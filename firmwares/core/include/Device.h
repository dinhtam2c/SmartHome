#pragma once

#include <cstdint>
#include <unordered_map>
#include <vector>

#include "Capability.h"
#include "Configuration.h"
#include "MqttManager.h"
#include "ProvisioningManager.h"
#include "WiFiManager.h"

class Device {
public:
    Device(const std::string& apSsid, const std::string& apPassword,
        const std::string& deviceName, const std::string& firmwareVersion,
        const std::vector<EndpointDefinition>& endpoints,
        const std::vector<CapabilityDefinition>& capabilities)
        : _apSsid(apSsid), _apPassword(apPassword), _deviceName(deviceName),
        _firmwareVersion(firmwareVersion), _capabilities(capabilities), _endpoints(endpoints) {
    }

    void begin();
    void loop();
    void publishCapabilityStates(bool forceAll = false);
    void resetConfiguration();

private:
    static constexpr unsigned long SYSTEM_STATE_SEND_INTERVAL_MS = 15000;
    static constexpr unsigned long AVAILABILITY_SEND_INTERVAL_MS = 15000;

    std::string _apSsid;
    std::string _apPassword;
    std::string _deviceName;
    std::string _firmwareVersion;
    std::vector<CapabilityDefinition> _capabilities;
    std::vector<EndpointDefinition> _endpoints;
    std::unordered_map<std::string, std::string> _lastCapabilityState;

    unsigned long _lastSystemSentAt = 0;
    unsigned long _lastAvailabilitySentAt = 0;

    Configuration _configuration;
    WiFiManager _wifiManager;
    ProvisioningManager _provisioningManager;
    MqttManager _mqttManager;

    void setupMqtt();
    void attachMqttHandlers();
    std::string topicAvailability() const;
    std::string topicSystemState() const;
    std::string topicCapabilityState() const;
    std::string topicCommand() const;
    std::string topicCommandResult() const;
    std::string deviceId() const;

    void publishAvailability(const char* state, bool retain = false);
    void publishSystemState();
    void handleCommandMessage(const std::string& payload);
    void publishCommandResult(const std::string& capabilityId,
        const std::string& correlationId,
        const std::string& operation,
        const std::string& status,
        const std::string& valueJson,
        const std::string& error);
    bool validateEndpointContracts() const;
    bool validateCapabilityContracts() const;
    const EndpointDefinition* findEndpoint(const std::string& endpointId) const;
    const CapabilityDefinition* findCapability(const std::string& capabilityId,
        const std::string& endpointId) const;
};
