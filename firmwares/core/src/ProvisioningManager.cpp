#include "ProvisioningManager.h"

#include <ArduinoJson.h>
#include <WiFi.h>

#include <algorithm>
#include <cctype>
#include <cstdio>
#include <unordered_map>
#include <unordered_set>

#include "ProvisioningCommandSchemaValidator.h"
#include "ProvisioningPortalRoutes.h"
#include "ProvisioningPortalView.h"
#include "utils.h"

namespace {
    std::string toLowerAscii(const std::string& value) {
        std::string normalized;
        normalized.reserve(value.size());
        for (char ch : value) {
            normalized.push_back(static_cast<char>(std::tolower(static_cast<unsigned char>(ch))));
        }
        return normalized;
    }

    std::string provisionClientIdFromMac(const std::string& macAddress) {
        std::string normalized = normalizeMacUpperWithColons(macAddress);
        std::string compact;
        compact.reserve(normalized.size());
        for (char ch : normalized) {
            if (ch == ':') {
                continue;
            }
            compact.push_back(ch);
        }
        return std::string("provision-client-") + compact;
    }

    const char* wifiAuthModeToString(wifi_auth_mode_t mode) {
        switch (mode) {
        case WIFI_AUTH_OPEN:
            return "OPEN";
        case WIFI_AUTH_WEP:
            return "WEP";
        case WIFI_AUTH_WPA_PSK:
            return "WPA-PSK";
        case WIFI_AUTH_WPA2_PSK:
            return "WPA2-PSK";
        case WIFI_AUTH_WPA_WPA2_PSK:
            return "WPA/WPA2-PSK";
        case WIFI_AUTH_WPA2_ENTERPRISE:
            return "WPA2-ENTERPRISE";
        case WIFI_AUTH_WPA3_PSK:
            return "WPA3-PSK";
        case WIFI_AUTH_WPA2_WPA3_PSK:
            return "WPA2/WPA3-PSK";
        case WIFI_AUTH_WAPI_PSK:
            return "WAPI-PSK";
        default:
            return "UNKNOWN";
        }
    }
}

void ProvisioningManager::begin(Configuration& configuration, WiFiManager& wifiManager,
    const std::string& apSsid, const std::string& apPassword,
    const std::string& deviceName, const std::string& firmwareVersion,
    const std::vector<EndpointDefinition>& endpoints,
    const std::vector<CapabilityDefinition>& capabilities) {
    _configuration = &configuration;
    _wifiManager = &wifiManager;
    _apSsid = apSsid;
    _apPassword = apPassword;
    _deviceName = deviceName;
    _firmwareVersion = firmwareVersion;
    _endpoints = endpoints;
    _capabilities = capabilities;

    Serial.println("[Provisioning] begin");
    Serial.printf("[Provisioning] deviceName=%s firmwareVersion=%s\r\n",
        _deviceName.c_str(), _firmwareVersion.c_str());

    if (_configuration == nullptr || _wifiManager == nullptr) {
        Serial.println("[Provisioning] unavailable: missing configuration or Wi-Fi manager");
        return;
    }

    WifiPreference wifiPreference = _configuration->wifi();
    ServerPreference serverPreference = _configuration->server();
    CredentialPreference credentialPreference = _configuration->credentials();

    bool hasServerConfig = !serverPreference.address.empty() && serverPreference.port > 0;
    bool hasCredentials = !credentialPreference.deviceId.empty() && !credentialPreference.token.empty();

    Serial.printf("[Provisioning] boot config: wifiSsid=%s wifiPassLen=%u server=%s port=%u deviceId=%s tokenLen=%u\r\n",
        wifiPreference.ssid.c_str(),
        static_cast<unsigned>(wifiPreference.password.length()),
        serverPreference.address.c_str(),
        serverPreference.port,
        credentialPreference.deviceId.c_str(),
        static_cast<unsigned>(credentialPreference.token.length()));

    configureProvisioningPortalRoutes(
        _server,
        [this]() { return renderPortalPage(); },
        [this]() { return renderProvisionStatus(); },
        [this]() { return renderWifiNetworksJson(); },
        [this]() { handleWifiScan(); },
        [this]() { handleSave(); },
        [this]() { handleNotFound(); });

    if (!hasServerConfig || !hasCredentials) {
        _state = hasServerConfig ? State::WaitingForWifi : State::ConfigRequired;
        if (_state == State::WaitingForWifi) {
            _message = "Waiting for Wi-Fi to request provisioning code...";
        }
        Serial.printf("[Provisioning] portal required (hasServerConfig=%d hasCredentials=%d), starting captive portal\r\n",
            hasServerConfig ? 1 : 0, hasCredentials ? 1 : 0);
        startPortal();

        if (_state == State::WaitingForWifi && _wifiManager->isConnected()) {
            _state = State::RequestingCode;
            _message = "Connected. Requesting provisioning code...";
            _requestPublished = false;
            _lastProvisionAction = 0;
            Serial.println("[Provisioning] Wi-Fi already connected at boot, starting provisioning request flow");
            ensureProvisionMqttStarted();
            requestProvisionCode();
        }
    } else {
        _state = State::Provisioned;
        Serial.println("[Provisioning] existing server and credentials found");
    }

    xTaskCreate(loopTask, "ProvisionLoop", 6144, this, 1, NULL);
}

void ProvisioningManager::waitForProvisioning() {
    Serial.println("[Provisioning] waiting for provisioning completion...");
    while (!isProvisioned()) {
        delay(500);
    }
    Serial.println("[Provisioning] provisioning complete");
}

bool ProvisioningManager::isProvisioned() const {
    return _configuration != nullptr &&
        _wifiManager != nullptr &&
        _wifiManager->isConnected() &&
        hasCompleteServerConfig() &&
        hasDeviceCredentials();
}

void ProvisioningManager::startPortal() {
    if (_portalActive) {
        return;
    }

    Serial.println("[Provisioning] starting captive portal AP");
    WiFi.mode(WIFI_AP_STA);
    WiFi.softAPConfig(IPAddress(192, 168, 4, 1), IPAddress(192, 168, 4, 1), IPAddress(255, 255, 255, 0));
    WiFi.softAP(_apSsid.c_str(), _apPassword.c_str());

    _dnsServer.start(53, "*", WiFi.softAPIP());
    _server.begin();
    _portalActive = true;

    Serial.printf("[Provisioning] AP SSID=%s IP=%s\r\n", _apSsid.c_str(), WiFi.softAPIP().toString().c_str());
}

void ProvisioningManager::stopPortal() {
    if (!_portalActive) {
        return;
    }

    Serial.println("[Provisioning] stopping captive portal");
    _dnsServer.stop();
    _server.stop();
    WiFi.softAPdisconnect(true);
    _portalActive = false;
}

ProvisioningPortalState ProvisioningManager::portalState() const {
    switch (_state) {
    case State::Idle:
        return ProvisioningPortalState::Idle;
    case State::ConfigRequired:
        return ProvisioningPortalState::ConfigRequired;
    case State::WaitingForWifi:
        return ProvisioningPortalState::WaitingForWifi;
    case State::RequestingCode:
        return ProvisioningPortalState::RequestingCode;
    case State::WaitingForApproval:
        return ProvisioningPortalState::WaitingForApproval;
    case State::Provisioned:
        return ProvisioningPortalState::Provisioned;
    case State::Error:
        return ProvisioningPortalState::Error;
    }

    return ProvisioningPortalState::Idle;
}

std::string ProvisioningManager::renderPortalPage() const {
    ProvisioningPortalViewModel model;
    if (_configuration != nullptr) {
        model.wifi = _configuration->wifi();
        model.server = _configuration->server();
    }
    model.state = portalState();
    model.message = _message;
    model.provisionCode = _provisionCode;
    return renderProvisioningPortalPage(model);
}

std::string ProvisioningManager::renderProvisionStatus() const {
    ProvisioningPortalViewModel model;
    model.state = portalState();
    model.message = _message;
    model.provisionCode = _provisionCode;
    return renderProvisioningStatusHtml(model);
}

std::string ProvisioningManager::renderWifiNetworksJson() const {
    JsonDocument payload;
    payload["scanInProgress"] = _wifiManager != nullptr ? _wifiManager->scanInProgress() : false;
    payload["lastScanAt"] = _lastWifiScanCompletedAt;

    JsonArray results = payload["results"].to<JsonArray>();
    size_t resultCount = std::min(_wifiScanResults.size(), WIFI_SCAN_MAX_RESULTS);
    for (size_t i = 0; i < resultCount; ++i) {
        const WiFiScanNetwork& network = _wifiScanResults[i];
        JsonObject item = results.add<JsonObject>();
        item["ssid"] = network.ssid.c_str();
        item["bssid"] = network.bssid.c_str();
        item["rssi"] = network.rssi;
        item["channel"] = network.channel;
        item["authMode"] = wifiAuthModeToString(network.authMode);
        item["hidden"] = network.hidden;
    }

    std::string json;
    serializeJson(payload, json);
    return json;
}

void ProvisioningManager::handleWifiScan() {
    if (_wifiManager == nullptr) {
        _server.send(500, "application/json", "{\"error\":\"wifi_manager_unavailable\"}");
        return;
    }

    if (_state != State::ConfigRequired) {
        _server.send(409, "application/json", "{\"error\":\"scan_not_allowed_in_current_state\"}");
        return;
    }

    bool started = _wifiManager->triggerScan();
    if (started) {
        _lastWifiScanRequestAt = millis();
    }

    _server.sendHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0", true);
    _server.sendHeader("Pragma", "no-cache", true);
    _server.sendHeader("Expires", "0", true);
    _server.send(started ? 202 : 200,
        "application/json",
        started ? "{\"status\":\"scan_started\"}" : "{\"status\":\"scan_in_progress\"}");
}

void ProvisioningManager::handleSave() {
    if (_configuration == nullptr || _wifiManager == nullptr) {
        Serial.println("[Provisioning] save rejected: provisioning unavailable");
        _server.send(500, "text/plain", "Provisioning unavailable");
        return;
    }

    std::string ssid = _server.arg("ssid").c_str();
    if (ssid.empty()) {
        ssid = _server.arg("ssidSelect").c_str();
    }
    if (ssid == "__other__") {
        ssid.clear();
    }
    std::string password = _server.arg("password").c_str();
    std::string serverAddress = _server.arg("server").c_str();
    std::string serverPortRaw = _server.arg("port").c_str();
    uint16_t serverPort = static_cast<uint16_t>(atoi(serverPortRaw.c_str()));

    if (ssid.empty() || password.empty() || serverAddress.empty() || serverPort == 0) {
        _state = State::ConfigRequired;
        _message = "All fields are required.";
        Serial.println("[Provisioning] save rejected: missing required fields");
        std::string page = renderPortalPage();
        _server.sendHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0", true);
        _server.sendHeader("Pragma", "no-cache", true);
        _server.sendHeader("Expires", "0", true);
        _server.send(400, "text/html", String(page.c_str()));
        return;
    }

    Serial.printf("[Provisioning] saving wifi ssid=%s server=%s port=%u\r\n",
        ssid.c_str(), serverAddress.c_str(), serverPort);
    _configuration->setWifi(ssid, password);
    _configuration->setServer(serverAddress, serverPort);
    _configuration->setCredentials("", "");

    _provisionCode.clear();
    _requestPublished = false;
    _lastProvisionAction = 0;
    _state = State::WaitingForWifi;
    _message = "Saved. Connecting to Wi-Fi...";

    _server.sendHeader("Location", "/", true);
    _server.send(303, "text/plain", "Saved");
    _wifiManager->requestReconnect();
}

void ProvisioningManager::handleNotFound() {
    if (_portalActive) {
        Serial.printf("[Provisioning] redirecting captive request to portal: %s\r\n",
            _server.uri().c_str());
        std::string location = std::string("http://") + WiFi.softAPIP().toString().c_str();
        _server.sendHeader("Location", String(location.c_str()), true);
        _server.send(302, "text/plain", "");
        return;
    }

    _server.send(404, "text/plain", "Not found");
}

bool ProvisioningManager::hasCompleteServerConfig() const {
    if (_configuration == nullptr) {
        return false;
    }

    ServerPreference serverPreference = _configuration->server();
    return !serverPreference.address.empty() && serverPreference.port > 0;
}

bool ProvisioningManager::hasDeviceCredentials() const {
    if (_configuration == nullptr) {
        return false;
    }

    CredentialPreference credentialPreference = _configuration->credentials();
    return !credentialPreference.deviceId.empty() && !credentialPreference.token.empty();
}

std::string ProvisioningManager::provisionResponseTopic() const {
    std::string macAddress = normalizeMacUpperWithColons(WiFi.macAddress().c_str());
    return std::string(PROVISION_TOPIC_PREFIX) + "/" + macAddress + "/response";
}

std::string ProvisioningManager::provisionRequestTopic() const {
    std::string macAddress = normalizeMacUpperWithColons(WiFi.macAddress().c_str());
    return std::string(PROVISION_TOPIC_PREFIX) + "/" + macAddress + "/request";
}

void ProvisioningManager::processProvisioning() {
    if (_configuration == nullptr || _wifiManager == nullptr || !_portalActive) {
        return;
    }

    unsigned long now = millis();

    if (_state == State::WaitingForWifi) {
        if (_wifiManager->isConnected()) {
            _state = State::RequestingCode;
            _message = "Connected. Requesting provisioning code...";
            _requestPublished = false;
            _lastProvisionAction = 0;
            Serial.println("[Provisioning] Wi-Fi connected, starting provisioning request flow");
            ensureProvisionMqttStarted();
            requestProvisionCode();
        }
        return;
    }

    if (_state == State::RequestingCode || _state == State::WaitingForApproval) {
        if (!_wifiManager->isConnected()) {
            _state = State::WaitingForWifi;
            _message = "Wi-Fi disconnected. Reconnecting...";
            _requestPublished = false;
            Serial.println("[Provisioning] Wi-Fi lost during provisioning, waiting to reconnect");
            return;
        }

        if (_state == State::WaitingForApproval) {
            return;
        }

        if (_requestPublished) {
            return;
        }

        if (now - _lastProvisionAction < PROVISION_RETRY_MS) {
            return;
        }

        _lastProvisionAction = now;
        requestProvisionCode();
    }
}

bool ProvisioningManager::requestProvisionCode() {
    ensureProvisionMqttStarted();

    if (_endpoints.empty()) {
        _state = State::Error;
        _message = "Provisioning requires at least one endpoint.";
        Serial.println("[Provisioning] reject request: endpoints are empty");
        return false;
    }

    if (_capabilities.empty()) {
        _state = State::Error;
        _message = "Provisioning requires at least one capability.";
        Serial.println("[Provisioning] reject request: capabilities are empty");
        return false;
    }

    std::unordered_set<std::string> endpointCodes;
    std::unordered_map<std::string, size_t> capabilityCountByEndpoint;

    for (const auto& endpoint : _endpoints) {
        if (endpoint.endpointId.empty()) {
            _state = State::Error;
            _message = "Endpoint endpointId cannot be empty in provisioning payload.";
            Serial.println("[Provisioning] reject request: endpoint.endpointId is empty");
            return false;
        }

        std::string normalizedEndpointCode = toLowerAscii(endpoint.endpointId);
        auto endpointInserted = endpointCodes.insert(normalizedEndpointCode);
        if (!endpointInserted.second) {
            _state = State::Error;
            _message = "Duplicate endpointId in provisioning payload.";
            Serial.printf("[Provisioning] reject request: duplicate endpointId=%s\r\n",
                endpoint.endpointId.c_str());
            return false;
        }
    }

    std::unordered_set<std::string> capabilityEndpointPairs;
    for (const auto& capability : _capabilities) {
        if (capability.capabilityId.empty()) {
            _state = State::Error;
            _message = "Provisioning payload has empty capabilityId.";
            Serial.println("[Provisioning] reject request: empty capabilityId");
            return false;
        }

        if (capability.capabilityVersion <= 0) {
            _state = State::Error;
            _message = "Capability '" + capability.capabilityId + "' has invalid capabilityVersion.";
            Serial.printf("[Provisioning] reject request: invalid capabilityVersion capabilityId=%s capabilityVersion=%d\r\n",
                capability.capabilityId.c_str(), capability.capabilityVersion);
            return false;
        }

        if (capability.endpointId.empty()) {
            _state = State::Error;
            _message = "Capability '" + capability.capabilityId + "' requires endpointId.";
            Serial.printf("[Provisioning] reject request: missing endpointId capabilityId=%s\r\n",
                capability.capabilityId.c_str());
            return false;
        }

        std::string normalizedEndpointId = toLowerAscii(capability.endpointId);

        if (endpointCodes.find(normalizedEndpointId) == endpointCodes.end()) {
            _state = State::Error;
            _message = "Capability '" + capability.capabilityId + "' references unknown endpointId '" + capability.endpointId + "'.";
            Serial.printf("[Provisioning] reject request: unknown endpointId=%s capabilityId=%s\r\n",
                capability.endpointId.c_str(), capability.capabilityId.c_str());
            return false;
        }

        std::string normalizedCapabilityId = toLowerAscii(capability.capabilityId);

        std::string pairKey = normalizedEndpointId + "|" + normalizedCapabilityId;
        auto pairInserted = capabilityEndpointPairs.insert(pairKey);
        if (!pairInserted.second) {
            _state = State::Error;
            _message = "Duplicate capabilityId + endpointId in provisioning payload.";
            Serial.printf("[Provisioning] reject request: duplicate pair capabilityId=%s endpointId=%s\r\n",
                capability.capabilityId.c_str(), capability.endpointId.c_str());
            return false;
        }

        capabilityCountByEndpoint[normalizedEndpointId]++;

        std::string schemaValidationError;
        if (!validateProvisioningCommandSchemaForOperations(capability, schemaValidationError)) {
            _state = State::Error;
            _message = "Capability '" + capability.capabilityId + "': " + schemaValidationError;
            Serial.printf("[Provisioning] reject request: invalid supportedOperations capabilityId=%s error=%s\r\n",
                capability.capabilityId.c_str(), schemaValidationError.c_str());
            return false;
        }
    }

    for (const auto& endpoint : _endpoints) {
        std::string normalizedEndpointCode = toLowerAscii(endpoint.endpointId);
        auto found = capabilityCountByEndpoint.find(normalizedEndpointCode);
        if (found == capabilityCountByEndpoint.end() || found->second == 0) {
            _state = State::Error;
            _message = "Endpoint '" + endpoint.endpointId + "' has no capabilities.";
            Serial.printf("[Provisioning] reject request: endpoint has no capabilities endpointId=%s\r\n",
                endpoint.endpointId.c_str());
            return false;
        }
    }

    if (!_provisionMqtt.isConnected()) {
        _message = "Waiting for provisioning MQTT connection...";
        Serial.println("[Provisioning] MQTT not connected yet, deferring request publish");
        return false;
    }

    std::string macAddress = normalizeMacUpperWithColons(WiFi.macAddress().c_str());

    JsonDocument payload;
    payload["name"] = _deviceName.c_str();
    payload["firmwareVersion"] = _firmwareVersion.c_str();
    payload["protocol"] = "DirectMqtt";

    JsonArray endpoints = payload["endpoints"].to<JsonArray>();
    for (const auto& endpoint : _endpoints) {
        std::string normalizedEndpointCode = toLowerAscii(endpoint.endpointId);
        JsonObject endpointItem = endpoints.add<JsonObject>();
        endpointItem["endpointId"] = endpoint.endpointId.c_str();
        if (!endpoint.name.empty()) {
            endpointItem["name"] = endpoint.name.c_str();
        } else {
            endpointItem["name"] = endpoint.endpointId.c_str();
        }

        JsonArray capabilities = endpointItem["capabilities"].to<JsonArray>();
        for (const auto& capability : _capabilities) {
            if (toLowerAscii(capability.endpointId) != normalizedEndpointCode) {
                continue;
            }

            JsonObject capabilityItem = capabilities.add<JsonObject>();
            capabilityItem["capabilityId"] = capability.capabilityId.c_str();
            capabilityItem["capabilityVersion"] = capability.capabilityVersion;

            JsonArray operations = capabilityItem["supportedOperations"].to<JsonArray>();
            for (const auto& operation : capability.supportedOperations) {
                operations.add(operation.c_str());
            }
        }
    }

    std::string body;
    serializeJson(payload, body);

    std::string requestTopic = provisionRequestTopic();

    Serial.printf("[Provisioning] publishing request topic=%s deviceName=%s macAddress=%s firmwareVersion=%s endpoints=%u\r\n",
        requestTopic.c_str(), _deviceName.c_str(), macAddress.c_str(), _firmwareVersion.c_str(),
        static_cast<unsigned>(_endpoints.size()));
    _provisionMqtt.publish(requestTopic, body, 1, false);
    _requestPublished = true;

    if (_state == State::RequestingCode) {
        _message = "Provision request sent. Waiting for code...";
    } else {
        _message = "Waiting for server to return device credentials...";
    }

    return true;
}

void ProvisioningManager::ensureProvisionMqttStarted() {
    if (_configuration == nullptr || _wifiManager == nullptr) {
        return;
    }

    if (_provisionMqttStarted) {
        return;
    }

    ServerPreference serverPreference = _configuration->server();
    if (serverPreference.address.empty() || serverPreference.port == 0) {
        return;
    }

    std::string responseTopic = provisionResponseTopic();

    _provisionMqtt.onConnected([this, responseTopic]() {
        Serial.println("[Provisioning] provisioning MQTT connected");
        Serial.printf("[Provisioning] subscribing to response topic: %s\r\n", responseTopic.c_str());
        _provisionMqtt.subscribe(responseTopic, 1);
        _requestPublished = false;

        if (_state == State::RequestingCode) {
            _lastProvisionAction = 0;
            requestProvisionCode();
        }
        });

    _provisionMqtt.onDisconnected([this](AsyncMqttClientDisconnectReason) {
        Serial.println("[Provisioning] provisioning MQTT disconnected");
        _requestPublished = false;
        if (_state == State::RequestingCode) {
            _message = "Provisioning MQTT disconnected. Waiting to reconnect...";
        }
        });

    _provisionMqtt.onMessage([this, responseTopic](const std::string& topic, const std::string& payload) {
        if (topic != responseTopic) {
            Serial.printf("[Provisioning] ignoring message on unexpected topic: %s\r\n", topic.c_str());
            return;
        }
        Serial.printf("[Provisioning] response received on %s\r\n", topic.c_str());
        handleProvisionMessage(payload);
        });

    _provisionMqtt.begin(
        _wifiManager,
        serverPreference.address,
        serverPreference.port,
        provisionClientIdFromMac(WiFi.macAddress().c_str()),
        true,
        nullptr,
        "",
        "");

    _provisionMqttStarted = true;
}

void ProvisioningManager::handleProvisionMessage(const std::string& payload) {
    JsonDocument doc;
    DeserializationError err = deserializeJson(doc, payload.c_str());
    if (err) {
        _message = "Invalid provisioning response payload.";
        return;
    }

    std::string provisionCode = doc["provisionCode"] | "";
    std::string deviceId = doc["deviceId"] | "";
    std::string accessToken = doc["accessToken"] | "";

    if (!provisionCode.empty()) {
        _provisionCode = provisionCode;
        _state = State::WaitingForApproval;
        _requestPublished = true;
        _message = "Code issued. Confirm on server and keep this page open.";
        Serial.printf("[Provisioning] code issued: %s\r\n", _provisionCode.c_str());
    }

    if (!deviceId.empty() && !accessToken.empty()) {
        _configuration->setCredentials(deviceId, accessToken);
        _state = State::Provisioned;
        _message = "Credentials received from server.";
        Serial.printf("[Provisioning] credentials received: deviceId=%s\r\n", deviceId.c_str());
        stopPortal();
        return;
    }
}

void ProvisioningManager::loopTask(void* self) {
    while (true) {
        ((ProvisioningManager*)self)->loop();
        vTaskDelay(200 / portTICK_PERIOD_MS);
    }
}

void ProvisioningManager::loop() {
    if (!_portalActive) {
        return;
    }

    _dnsServer.processNextRequest();
    _server.handleClient();
    processWifiScan();
    processProvisioning();
}

void ProvisioningManager::processWifiScan() {
    if (_wifiManager == nullptr) {
        return;
    }

    // Keep scans only on the configuration screen. Scanning during provisioning
    // can destabilize STA/MQTT in AP+STA mode on ESP32.
    if (_state != State::ConfigRequired) {
        return;
    }

    uint32_t currentVersion = _wifiManager->scanResultVersion();
    if (currentVersion != _wifiScanResultVersion) {
        _wifiScanResults = _wifiManager->scanResults();
        _wifiScanResultVersion = currentVersion;
        _lastWifiScanCompletedAt = millis();
    }

    unsigned long now = millis();
    if (_wifiManager->scanInProgress()) {
        return;
    }

    if (_lastWifiScanRequestAt != 0 && now - _lastWifiScanRequestAt < WIFI_SCAN_INTERVAL_MS) {
        return;
    }

    if (_wifiManager->triggerScan()) {
        _lastWifiScanRequestAt = now;
    }
}
