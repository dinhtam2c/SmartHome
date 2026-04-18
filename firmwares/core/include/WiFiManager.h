#pragma once

#include <Arduino.h>
#include <WiFi.h>

#include <string>
#include <vector>

#include "Configuration.h"

struct WiFiScanNetwork {
    std::string ssid;
    std::string bssid;
    int32_t rssi = 0;
    int32_t channel = 0;
    wifi_auth_mode_t authMode = WIFI_AUTH_OPEN;
    bool hidden = false;
};

class WiFiManager {
public:
    WiFiManager() = default;

    void begin(Configuration& configuration);
    void requestReconnect();

    void waitForConnection();
    bool isConnected();
    void onConnected(std::function<void()> callback);
    void onDisconnected(std::function<void()> callback);

    bool triggerScan();
    bool scanInProgress() const;
    uint32_t scanResultVersion() const;
    const std::vector<WiFiScanNetwork>& scanResults() const;

private:
    static constexpr unsigned long CONNECT_TIMEOUT_MS = 20000;
    static constexpr unsigned long RETRY_INTERVAL_MS = 10000;

    Configuration* _configuration = nullptr;
    bool _connecting = false;
    unsigned long _connectStart = 0;
    unsigned long _lastTry = 0;
    unsigned long _retryInterval = RETRY_INTERVAL_MS;

    std::function<void()> _onConnected;
    std::function<void()> _onDisconnected;

    bool _scanInProgress = false;
    uint32_t _scanResultVersion = 0;
    std::vector<WiFiScanNetwork> _scanResults;

    void handleEvent(WiFiEvent_t event);
    void connectStation();
    bool hasCompleteWifiConfig() const;
    void updateScanResults();

    static void loopTask(void* self);
    void loop();
};
