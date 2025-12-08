#pragma once

#include <Arduino.h>
#include <WiFi.h>

#include "INetworkManager.h"

class WiFiManager : public INetworkManager {
public:
    void begin(const char* ssid, const char* password,
            unsigned long initialInterval = 10000, unsigned long maxInterval = 30000, unsigned long intervalStep = 5000);

    void begin() override;

    void waitForConnection() override;
    bool isConnected() override;
    void onConnected(std::function<void()> callback) override;
    void onDisconnected(std::function<void()> callback) override;

private:
    const char* _ssid;
    const char* _password;
    
    unsigned long _lastTry;
    unsigned long _retryInterval;
    unsigned long _initialInterval;
    unsigned long _maxInterval;
    unsigned long _intervalStep;

    std::function<void()> _onConnected;
    std::function<void()> _onDisconnected;
    
    void handleEvent(WiFiEvent_t event);

    static void loopTask(void* self);
    void loop();
};
