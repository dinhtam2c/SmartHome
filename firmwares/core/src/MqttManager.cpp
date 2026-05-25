#include "MqttManager.h"

#include <Arduino.h>
#include <ESPmDNS.h>
#include <WiFi.h>

#include <algorithm>
#include <cctype>

namespace {
    std::string toLowerAscii(const std::string& value) {
        std::string normalized;
        normalized.reserve(value.size());
        for (char ch : value) {
            normalized.push_back(static_cast<char>(std::tolower(static_cast<unsigned char>(ch))));
        }
        return normalized;
    }

    bool endsWithLocalDomain(const std::string& host) {
        std::string normalized = toLowerAscii(host);
        return normalized.length() > 6 &&
            normalized.compare(normalized.length() - 6, 6, ".local") == 0;
    }

    std::string localMdnsHostname() {
        std::string mac = WiFi.macAddress().c_str();
        std::string compact;
        compact.reserve(mac.length());
        for (char ch : mac) {
            if (ch == ':') {
                continue;
            }
            compact.push_back(static_cast<char>(std::tolower(static_cast<unsigned char>(ch))));
        }

        if (compact.empty()) {
            return "smarthome-device";
        }

        return std::string("smarthome-device-") + compact;
    }

    bool ensureMdnsStarted() {
        static bool started = false;

        if (started) {
            return true;
        }

        std::string hostname = localMdnsHostname();
        started = MDNS.begin(hostname.c_str());
        if (started) {
            Serial.printf("[MQTT] mDNS started as %s.local\r\n", hostname.c_str());
        } else {
            Serial.println("[MQTT] mDNS start failed");
        }

        return started;
    }
}

void MqttManager::begin(WiFiManager* wifiManager, const std::string& host, uint16_t port,
    const std::string& clientId, bool cleanSession, Will* will, const std::string& username, const std::string& password) {
    _wifiManager = wifiManager;
    _host = host;
    _port = port;
    _clientId = clientId;
    _cleanSession = cleanSession;
    _username = username;
    _password = password;
    _stopRequested.store(false);
    _connecting.store(false);
    _wifiWasConnected = false;
    _wifiConnectionEpoch = _wifiManager != nullptr ? _wifiManager->connectionEpoch() : 0;
    _nextTryAt = 0;
    _retryInterval = INITIAL_RETRY_INTERVAL_MS;

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
    _client.setCleanSession(_cleanSession);

    if (will) {
        _will = *will;
        _client.setWill(_will.topic.c_str(), _will.qos, _will.retain, _will.payload.c_str(), _will.payload.length());
    }

    xTaskCreate(loopTask, "MqttLoop", 4096, this, 1, NULL);
}

void MqttManager::stop() {
    // May be called from an AsyncMqttClient callback. The manager task owns the
    // actual socket close to avoid re-entering the client from its callback.
    _stopRequested.store(true);
}

void MqttManager::loopTask(void* self) {
    MqttManager* manager = static_cast<MqttManager*>(self);
    while (!manager->_stopRequested.load()) {
        manager->loop();
        vTaskDelay(250 / portTICK_PERIOD_MS);
    }

    if (manager->_client.connected() || manager->_connecting.load()) {
        manager->_client.disconnect(true);
    }
    manager->_connecting.store(false);
    vTaskDelete(nullptr);
}

void MqttManager::loop() {
    unsigned long now = millis();
    bool wifiConnected = _wifiManager != nullptr && _wifiManager->isConnected();
    uint32_t wifiConnectionEpoch =
        _wifiManager != nullptr ? _wifiManager->connectionEpoch() : 0;

    if (wifiConnectionEpoch != _wifiConnectionEpoch) {
        _wifiConnectionEpoch = wifiConnectionEpoch;
        if (_wifiWasConnected && (_client.connected() || _connecting.load())) {
            Serial.println("[MQTT] Wi-Fi connection changed; resetting MQTT transport");
            _client.disconnect(true);
            _connecting.store(false);
            _wifiWasConnected = false;
            _retryInterval = INITIAL_RETRY_INTERVAL_MS;
            _nextTryAt = now + INITIAL_RETRY_INTERVAL_MS;
        }
    }

    if (_connectedEventPending.exchange(false)) {
        _retryInterval = INITIAL_RETRY_INTERVAL_MS;
        _nextTryAt = 0;
    }

    if (_disconnectedEventPending.exchange(false) && wifiConnected && !_client.connected()) {
        scheduleRetry(now);
    }

    if (!wifiConnected) {
        if (_wifiWasConnected || _client.connected() || _connecting.load()) {
            Serial.println("[MQTT] Wi-Fi unavailable; closing MQTT transport");
            _client.disconnect(true);
        }
        _connecting.store(false);
        _wifiWasConnected = false;
        _nextTryAt = 0;
        return;
    }

    if (!_wifiWasConnected) {
        Serial.println("[MQTT] Wi-Fi available; MQTT reconnect scheduled");
        _wifiWasConnected = true;
        _connecting.store(false);
        _retryInterval = INITIAL_RETRY_INTERVAL_MS;
        if (_nextTryAt == 0) {
            _nextTryAt = now;
        }
    }

    if (isConnected()) {
        return;
    }

    if (_connecting.load()) {
        if (now - _connectStartedAt >= CONNECT_TIMEOUT_MS) {
            Serial.println("[MQTT] connection attempt timed out");
            _client.disconnect(true);
            _connecting.store(false);
            scheduleRetry(now);
        }
        return;
    }

    if (_nextTryAt == 0 || deadlineReached(now, _nextTryAt)) {
        Serial.println("[MQTT] reconnecting...");
        connect();
    }
}

void MqttManager::connect() {
    if (_wifiManager != nullptr && !_wifiManager->isConnected()) {
        return;
    }

    if (!configureServerEndpoint()) {
        scheduleRetry(millis());
        return;
    }

    if (_wifiManager != nullptr && !_wifiManager->isConnected()) {
        scheduleRetry(millis());
        return;
    }

    _connectStartedAt = millis();
    _connecting.store(true);
    _nextTryAt = 0;
    _client.connect();
}

void MqttManager::scheduleRetry(unsigned long now) {
    if (_nextTryAt != 0 && !deadlineReached(now, _nextTryAt)) {
        return;
    }
    Serial.printf("[MQTT] next attempt in %lu ms\r\n", _retryInterval);
    _nextTryAt = now + _retryInterval;
    _retryInterval = std::min(_retryInterval * 2, MAX_RETRY_INTERVAL_MS);
}

bool MqttManager::configureServerEndpoint() {
    IPAddress brokerIp;
    if (brokerIp.fromString(_host.c_str())) {
        Serial.printf("[MQTT] using broker IP: %s:%u\r\n", brokerIp.toString().c_str(), _port);
        _client.setServer(brokerIp, _port);
        return true;
    }

    if (endsWithLocalDomain(_host)) {
        if (!ensureMdnsStarted()) {
            return false;
        }

        std::string mdnsHost = _host.substr(0, _host.length() - 6);
        Serial.printf("[MQTT] resolving broker via mDNS: %s.local\r\n", mdnsHost.c_str());
        brokerIp = MDNS.queryHost(mdnsHost.c_str(), 3000);
        if (!brokerIp) {
            Serial.printf("[MQTT] mDNS resolve failed: %s\r\n", _host.c_str());
            if (!_lastResolvedBrokerIp) {
                return false;
            }

            Serial.printf("[MQTT] using last resolved broker IP: %s:%u\r\n",
                _lastResolvedBrokerIp.toString().c_str(), _port);
            _client.setServer(_lastResolvedBrokerIp, _port);
            return true;
        }

        Serial.printf("[MQTT] mDNS resolved %s -> %s\r\n", _host.c_str(), brokerIp.toString().c_str());
        _lastResolvedBrokerIp = brokerIp;
        _client.setServer(brokerIp, _port);
        return true;
    }

    Serial.printf("[MQTT] using broker hostname via DNS: %s:%u\r\n", _host.c_str(), _port);
    _client.setServer(_host.c_str(), _port);
    return true;
}

void MqttManager::subscribe(const std::string& topic, uint8_t qos) {
    bool exists = false;
    for (auto& item : _topics) {
        if (item.name == topic) {
            item.qos = qos;
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
    if (!isConnected()) {
        Serial.printf("[MQTT] publish skipped while disconnected: %s\r\n", topic.c_str());
        return 0;
    }

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

    _connecting.store(false);
    _connectedEventPending.store(true);
    if (_onConnected)
        _onConnected();
}

void MqttManager::handleDisconnect(AsyncMqttClientDisconnectReason reason) {
    Serial.printf("[MQTT] disconnected (reason=%u)\r\n", static_cast<unsigned>(reason));
    _connecting.store(false);
    _disconnectedEventPending.store(true);
    _incomingTopic.clear();
    _incomingPayload.clear();
    _incomingTotal = 0;
    if (_onDisconnect)
        _onDisconnect(reason);
}

void MqttManager::handleMessage(char* topic, char* payload, AsyncMqttClientMessageProperties properties, size_t len, size_t index, size_t total) {
    (void)properties;

    if (total == 0 || total > MAX_INCOMING_MESSAGE_SIZE || index + len > total) {
        Serial.printf("[MQTT] invalid incoming message size: index=%u len=%u total=%u\r\n",
            static_cast<unsigned>(index),
            static_cast<unsigned>(len),
            static_cast<unsigned>(total));
        _incomingTopic.clear();
        _incomingPayload.clear();
        _incomingTotal = 0;
        return;
    }

    std::string currentTopic = topic != nullptr ? topic : "";
    if (index == 0) {
        _incomingTopic = currentTopic;
        _incomingPayload.clear();
        _incomingPayload.reserve(total);
        _incomingTotal = total;
    }

    if (_incomingTopic != currentTopic || _incomingTotal != total ||
        index != _incomingPayload.size()) {
        Serial.println("[MQTT] discarded out-of-order message fragments");
        _incomingTopic.clear();
        _incomingPayload.clear();
        _incomingTotal = 0;
        return;
    }

    _incomingPayload.append(payload, len);
    if (_incomingPayload.size() == _incomingTotal) {
        if (_onMessage) {
            _onMessage(_incomingTopic, _incomingPayload);
        }
        _incomingTopic.clear();
        _incomingPayload.clear();
        _incomingTotal = 0;
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

bool MqttManager::deadlineReached(unsigned long now, unsigned long deadline) {
    return static_cast<int32_t>(now - deadline) >= 0;
}
