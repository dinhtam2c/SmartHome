#include "gateway/MqttManager.h"

#include <Arduino.h>
#include <lwip/dns.h>

#include "gateway/config.h"

void MqttManager::begin(INetworkManager* networkManager, const std::string& host, uint16_t port,
    const std::string& clientId, bool cleanSession, Will* will) {
    _networkManager = networkManager;
    _host = host;
    _port = port;
    _clientId = clientId;
    _cleanSession = cleanSession;

    _client.onConnect([this](bool sessionPresent) { handleConnect(sessionPresent); });
    _client.onDisconnect([this](AsyncMqttClientDisconnectReason reason) { handleDisconnect(reason); });
    _client.onMessage([this](char* topic, char* payload, AsyncMqttClientMessageProperties properties, size_t len, size_t index, size_t total) {
        handleMessage(topic, payload, properties, len, index, total);
        });

    if (clientId.length() > 0) {
        _client.setClientId(_clientId.c_str());
    }
    _client.setServer(_host.c_str(), _port);
    _client.setCleanSession(_cleanSession);

    if (will) {
        _will = *will;
        _client.setWill(_will.topic.c_str(), _will.qos, _will.retain, _will.payload.c_str(), _will.payload.length());
    }

    connect();  // async
    xTaskCreate(loopTask, "MqttLoop", 2048, this, 1, NULL);
}

void MqttManager::loopTask(void* self) {
    while (true) {
        ((MqttManager*)self)->loop();
        // TODO: check stack size
        vTaskDelay(500 / portTICK_PERIOD_MS);
    }
}

void MqttManager::loop() {
    unsigned long now = millis();

    if (!isConnected() && _networkManager->isConnected() && !trying && now - _lastTry >= _retryInterval) {
        Serial.println("Reconnecting MQTT...");

        connect();

        _lastTry = now;
        _retryInterval = min(_retryInterval + _intervalStep, _maxInterval);
    }
}

void MqttManager::connect() {
    trying = true;
    _client.connect();
}

void MqttManager::subscribe(const std::string& topic, uint8_t qos) {
    _topics.push_back({ topic, qos });
    // TODO: if connected then sub right away
}

void MqttManager::publish(const std::string& topic, const std::string& payload, uint8_t qos, bool retain) {
    Serial.printf("Publishing to topic: %s\r\n", topic.c_str());
    //Serial.printf("Payload: %s\r\n", payload.c_str());

    _client.publish(topic.c_str(), qos, retain, payload.c_str(), payload.length());
}

void MqttManager::handleConnect(bool sessionPresent) {
    Serial.println("Connected to MQTT");
    Serial.printf("Host: %s\r\n", _host.c_str());
    Serial.printf("sessionPresent = %d\r\n", sessionPresent);

    // TODO: write onSubscribe to confirm subscriptions + timeout and resub
    for (auto topic : _topics) {
        Serial.printf("Subscribing: %s\r\n", topic.name.c_str());
        _client.subscribe(topic.name.c_str(), topic.qos);
    }

    _retryInterval = _initialInterval;
    trying = false;
    if (_onConnected)
        _onConnected();
}

void MqttManager::handleDisconnect(AsyncMqttClientDisconnectReason reason) {
    Serial.println("MQTT disconnected");
    trying = false;
    if (_onDisconnect)
        _onDisconnect(reason);
}

void MqttManager::handleMessage(char* topic, char* payload, AsyncMqttClientMessageProperties properties, size_t len, size_t index, size_t total) {
    if (_onMessage) {
        std::string messagePayload(payload, len);
        _onMessage(topic, messagePayload);
    }
}

bool MqttManager::isConnected() {
    return _client.connected();
}

void MqttManager::onConnected(std::function<void()> callback) {
    _onConnected = callback;
}

void MqttManager::onDisconnected(std::function<void(AsyncMqttClientDisconnectReason)> callback) {
    _onDisconnect = callback;
}

void MqttManager::onMessage(std::function<void(const std::string&, const std::string&)> callback) {
    _onMessage = callback;
}

void MqttManager::waitForConnection(int timeout) {
    Serial.println("Waiting for MQTT connection...");
    while (!isConnected()) {
        delay(500);
        Serial.print('.');
    }
}
