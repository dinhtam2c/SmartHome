#pragma once

#include <Arduino.h>

#include <AsyncMqttClient.h>
#include <vector>

#include "INetworkManager.h"

struct MqttTopic {
    std::string name;
    int qos;
};

struct Will {
    std::string topic;
    std::string payload;
    int qos;
    bool retain;
};

class MqttManager {
public:
    void begin(INetworkManager* networkManager, const std::string& host, uint16_t port,
        const std::string& clientId = "", bool cleanSession = true, Will* will = nullptr);

    void subscribe(const std::string& topic, uint8_t qos = 1);
    void publish(const std::string& topic, const std::string& payload, uint8_t qos = 1, bool retain = false);

    bool isConnected();

    void onConnected(std::function<void()> callback);
    void onDisconnected(std::function<void(AsyncMqttClientDisconnectReason)> callback);
    void onMessage(std::function<void(const std::string&, const std::string&)> callback);

    void waitForConnection(int timeout);

private:
    AsyncMqttClient _client;
    INetworkManager* _networkManager;

    std::string _host;
    uint16_t _port;
    std::string _clientId;
    bool _cleanSession;
    Will _will;

    std::vector<MqttTopic> _topics;

    bool trying = false;
    unsigned long _lastTry = 0;
    unsigned long _retryInterval = 5000;
    unsigned long _initialInterval = 5000;
    unsigned long _maxInterval = 30000;
    unsigned long _intervalStep = 5000;

    std::function<void()> _onConnected;
    std::function<void(AsyncMqttClientDisconnectReason)> _onDisconnect;
    std::function<void(const std::string&, const std::string&)> _onMessage;

    void connect();

    void handleConnect(bool sessionPresent);
    void handleDisconnect(AsyncMqttClientDisconnectReason reason);
    void handleMessage(char* topic, char* payload, AsyncMqttClientMessageProperties properties,
        size_t len, size_t index, size_t total);

    static void loopTask(void* self);
    void loop();
};
