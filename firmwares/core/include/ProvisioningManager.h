#pragma once

#include <Arduino.h>
#include <DNSServer.h>
#include <WebServer.h>

#include <string>
#include <vector>

#include "Capability.h"
#include "Configuration.h"
#include "MqttManager.h"
#include "WiFiManager.h"

enum class ProvisioningPortalState;

class ProvisioningManager {
public:
    ProvisioningManager() = default;

    void begin(Configuration& configuration, WiFiManager& wifiManager,
        const std::string& apSsid, const std::string& apPassword,
        const std::string& deviceName, const std::string& firmwareVersion,
        const std::vector<EndpointDefinition>& endpoints,
        const std::vector<CapabilityDefinition>& capabilities);
    void waitForProvisioning();
    bool isProvisioned() const;

private:
    static constexpr const char* PROVISION_TOPIC_PREFIX = "home/provision";

    enum class State {
        Idle,
        ConfigRequired,
        WaitingForWifi,
        RequestingCode,
        WaitingForApproval,
        Provisioned,
        Error,
    };

    static constexpr unsigned long PROVISION_RETRY_MS = 5000;
    static constexpr unsigned long WIFI_SCAN_INTERVAL_MS = 10000;
    static constexpr size_t WIFI_SCAN_MAX_RESULTS = 20;

    Configuration* _configuration = nullptr;
    WiFiManager* _wifiManager = nullptr;
    std::string _apSsid;
    std::string _apPassword;
    std::string _deviceName;
    std::string _firmwareVersion;
    std::vector<EndpointDefinition> _endpoints;
    std::vector<CapabilityDefinition> _capabilities;

    bool _portalActive = false;
    State _state = State::Idle;
    std::string _provisionCode;
    std::string _message;
    unsigned long _lastProvisionAction = 0;
    unsigned long _lastWifiScanRequestAt = 0;
    unsigned long _lastWifiScanCompletedAt = 0;
    bool _requestPublished = false;
    bool _provisionMqttStarted = false;
    uint32_t _wifiScanResultVersion = 0;
    std::vector<WiFiScanNetwork> _wifiScanResults;

    DNSServer _dnsServer;
    WebServer _server{ 80 };
    MqttManager _provisionMqtt;

    void startPortal();
    void stopPortal();
    ProvisioningPortalState portalState() const;
    std::string renderPortalPage() const;
    std::string renderProvisionStatus() const;
    std::string renderWifiNetworksJson() const;
    void handleWifiScan();
    void handleSave();
    void handleNotFound();

    bool hasCompleteServerConfig() const;
    bool hasDeviceCredentials() const;
    std::string provisionRequestTopic() const;
    std::string provisionResponseTopic() const;
    void processProvisioning();
    void processWifiScan();
    bool requestProvisionCode();
    void ensureProvisionMqttStarted();
    void handleProvisionMessage(const std::string& payload);

    static void loopTask(void* self);
    void loop();
};
