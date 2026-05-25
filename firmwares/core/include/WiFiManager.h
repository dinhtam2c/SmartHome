#pragma once

#include <Arduino.h>
#include <WiFi.h>
#include <freertos/queue.h>
#include <freertos/semphr.h>

#include <atomic>
#include <functional>
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
    uint32_t connectionEpoch() const;
    void onConnected(std::function<void()> callback);
    void onDisconnected(std::function<void()> callback);

    bool triggerScan();
    bool scanInProgress() const;
    uint32_t scanResultVersion() const;
    std::vector<WiFiScanNetwork> scanResults() const;

private:
    struct NetworkEvent {
        bool connected;
        uint8_t disconnectReason;
    };

    static constexpr unsigned long CONNECT_TIMEOUT_MS = 20000;
    static constexpr unsigned long INITIAL_RETRY_INTERVAL_MS = 2000;
    static constexpr unsigned long MAX_RETRY_INTERVAL_MS = 30000;
    static constexpr unsigned long RECONNECT_SETTLE_MS = 500;
    static constexpr unsigned long DISCONNECT_LOG_COOLDOWN_MS = 2000;

    Configuration* _configuration = nullptr;
    WifiPreference _wifiPreference;
    bool _connecting = false;
    bool _wasConnected = false;
    bool _restartPending = false;
    unsigned long _connectStart = 0;
    unsigned long _restartAt = 0;
    unsigned long _nextTryAt = 0;
    unsigned long _retryInterval = INITIAL_RETRY_INTERVAL_MS;
    unsigned long _lastDisconnectLog = 0;

    std::atomic<bool> _reconnectRequested{ false };
    std::atomic<uint32_t> _connectionEpoch{ 0 };
    QueueHandle_t _networkEventQueue = nullptr;

    std::function<void()> _onConnected;
    std::function<void()> _onDisconnected;

    mutable SemaphoreHandle_t _scanMutex = nullptr;
    bool _scanInProgress = false;
    uint32_t _scanResultVersion = 0;
    std::vector<WiFiScanNetwork> _scanResults;

    void handleEvent(WiFiEvent_t event, WiFiEventInfo_t info);
    void processNetworkEvents(unsigned long now);
    void connectStation();
    void scheduleRetry(unsigned long now);
    bool hasCompleteWifiConfig() const;
    void updateScanResults();
    static bool deadlineReached(unsigned long now, unsigned long deadline);

    static void loopTask(void* self);
    void loop();
};
