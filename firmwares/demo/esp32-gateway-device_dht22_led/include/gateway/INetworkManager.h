#pragma once

#include <Arduino.h>

// Generic network interface
class INetworkManager {
public:
    virtual ~INetworkManager() = default;

    virtual void begin() = 0;

    virtual bool isConnected() = 0;

    virtual void waitForConnection() = 0;

    virtual void onConnected(std::function<void()> callback) = 0;
    virtual void onDisconnected(std::function<void()> callback) = 0;
};
