#include "MqttManager.h"

#include <Arduino.h>
#include <lwip/dns.h>

void MqttManager::begin(WiFiManager* wifiManager, const std::string& host, uint16_t port,
    const std::string& clientId, bool cleanSession, Will* will, const std::string& username, const std::string& password) {
    _wifiManager = wifiManager;
    _host = host;
    _port = port;
    _clientId = clientId;
    _cleanSession = cleanSession;
    _username = username;
    _password = password;

    _client.onConnect([this](bool sessionPresent) { handleConnect(sessionPresent); });
    _client.onDisconnect([this](AsyncMqttClientDisconnectReason reason) { handleDisconnect(reason); });
    _client.onMessage([this](char* topic, char* payload, AsyncMqttClientMessageProperties properties, size_t len, size_t index, size_t total) {
        handleMessage(topic, payload, properties, len, index, total);
        });

    if (clientId.length() > 0) {
        _client.setClientId(_clientId.c_str());
    }
    if (username.length() > 0) {
        _client.setCredentials(_username.c_str(), _password.length() > 0 ? _password.c_str() : nullptr);
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

    if (!isConnected() && _wifiManager != nullptr && _wifiManager->isConnected() && !trying && now - _lastTry >= _retryInterval) {
        Serial.println("[MQTT] reconnecting...");

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
    bool exists = false;
    for (const auto& item : _topics) {
        if (item.name == topic) {
            exists = true;
            break;
        }
    }

    if (!exists) {
        _topics.push_back({ topic, qos });
    }

    if (isConnected()) {
        Serial.printf("[MQTT] subscribing immediately: %s\r\n", topic.c_str());
        _client.subscribe(topic.c_str(), qos);
    }
}

uint16_t MqttManager::publish(const std::string& topic, const std::string& payload, uint8_t qos, bool retain) {
    Serial.printf("[MQTT] publishing topic: %s\r\n", topic.c_str());
    //Serial.printf("Payload: %s\r\n", payload.c_str());

    return _client.publish(topic.c_str(), qos, retain, payload.c_str(), payload.length());
}

void MqttManager::handleConnect(bool sessionPresent) {
    Serial.println("[MQTT] connected");
    Serial.printf("[MQTT] host: %s\r\n", _host.c_str());
    Serial.printf("[MQTT] sessionPresent = %d\r\n", sessionPresent ? 1 : 0);

    // TODO: write onSubscribe to confirm subscriptions + timeout and resub
    for (auto topic : _topics) {
        Serial.printf("[MQTT] subscribing: %s\r\n", topic.name.c_str());
        _client.subscribe(topic.name.c_str(), topic.qos);
    }

    _retryInterval = _initialInterval;
    trying = false;
    if (_onConnected)
        _onConnected();
}

void MqttManager::handleDisconnect(AsyncMqttClientDisconnectReason reason) {
    Serial.println("[MQTT] disconnected");
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
    Serial.println("[MQTT] waiting for connection...");
    unsigned long startTime = millis();
    while (!isConnected()) {
        if (timeout > 0 && (millis() - startTime) >= static_cast<unsigned long>(timeout)) {
            Serial.println("\n[MQTT] connection timeout");
            return;
        }
        delay(500);
        Serial.print('.');
    }
    Serial.println("\n[MQTT] connected");
}
