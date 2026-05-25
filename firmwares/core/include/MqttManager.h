#pragma once

#include <Arduino.h>

#include <AsyncMqttClient.h>
#include <atomic>
#include <functional>
#include <vector>

#include "WiFiManager.h"

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
    void begin(WiFiManager* wifiManager, const std::string& host, uint16_t port,
        const std::string& clientId = "", bool cleanSession = true, Will* will = nullptr,
        const std::string& username = "", const std::string& password = "");
    void stop();

    void subscribe(const std::string& topic, uint8_t qos = 1);
    uint16_t publish(const std::string& topic, const std::string& payload, uint8_t qos = 1, bool retain = false);

    bool isConnected();

    void onConnected(std::function<void()> callback);
    void onDisconnected(std::function<void(AsyncMqttClientDisconnectReason)> callback);
    void onMessage(std::function<void(const std::string&, const std::string&)> callback);

    void waitForConnection(int timeout);

private:
    static constexpr unsigned long CONNECT_TIMEOUT_MS = 15000;
    static constexpr unsigned long INITIAL_RETRY_INTERVAL_MS = 1000;
    static constexpr unsigned long MAX_RETRY_INTERVAL_MS = 30000;
    static constexpr size_t MAX_INCOMING_MESSAGE_SIZE = 16384;

    AsyncMqttClient _client;
    WiFiManager* _wifiManager = nullptr;

    std::string _host;
    uint16_t _port = 0;
    std::string _clientId;
    bool _cleanSession = true;
    Will _will;
    std::string _username;
    std::string _password;
    IPAddress _lastResolvedBrokerIp;

    std::vector<MqttTopic> _topics;

    std::atomic<bool> _connecting{ false };
    std::atomic<bool> _stopRequested{ false };
    std::atomic<bool> _connectedEventPending{ false };
    std::atomic<bool> _disconnectedEventPending{ false };
    bool _wifiWasConnected = false;
    uint32_t _wifiConnectionEpoch = 0;
    unsigned long _connectStartedAt = 0;
    unsigned long _nextTryAt = 0;
    unsigned long _retryInterval = INITIAL_RETRY_INTERVAL_MS;

    std::string _incomingTopic;
    std::string _incomingPayload;
    size_t _incomingTotal = 0;

    std::function<void()> _onConnected;
    std::function<void(AsyncMqttClientDisconnectReason)> _onDisconnect;
    std::function<void(const std::string&, const std::string&)> _onMessage;

    void connect();
    void scheduleRetry(unsigned long now);
    bool configureServerEndpoint();
    static bool deadlineReached(unsigned long now, unsigned long deadline);

    void handleConnect(bool sessionPresent);
    void handleDisconnect(AsyncMqttClientDisconnectReason reason);
    void handleMessage(char* topic, char* payload, AsyncMqttClientMessageProperties properties,
        size_t len, size_t index, size_t total);

    static void loopTask(void* self);
    void loop();
};
