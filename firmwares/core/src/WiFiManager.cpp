#include "WiFiManager.h"

#include <Arduino.h>

#include <algorithm>

void WiFiManager::begin(Configuration& configuration) {
    _configuration = &configuration;

    if (_configuration == nullptr) {
        return;
    }

    WiFi.onEvent([this](WiFiEvent_t event, WiFiEventInfo_t) { handleEvent(event); });
    WiFi.mode(WIFI_STA);
    connectStation();

    xTaskCreate(loopTask, "WiFiLoop", 4096, this, 1, NULL);
}

void WiFiManager::requestReconnect() {
    _connecting = false;
    _lastTry = 0;
    connectStation();
}

void WiFiManager::loopTask(void* self) {
    while (true) {
        ((WiFiManager*)self)->loop();
        vTaskDelay(500 / portTICK_PERIOD_MS);
    }
}

void WiFiManager::loop() {
    unsigned long now = millis();

    if (_configuration == nullptr) {
        return;
    }

    updateScanResults();

    // Do not run reconnect attempts until Wi-Fi credentials are configured.
    if (!hasCompleteWifiConfig()) {
        _connecting = false;
        return;
    }

    if (isConnected()) {
        return;
    }

    if (_connecting && now - _connectStart >= CONNECT_TIMEOUT_MS) {
        _connecting = false;
    }

    if (!_connecting && now - _lastTry >= _retryInterval) {
        Serial.println("[WiFi] reconnecting...");
        connectStation();
        _lastTry = now;
    }
}

void WiFiManager::waitForConnection() {
    Serial.println("[WiFi] waiting for connection...");
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print('.');
    }
}

void WiFiManager::handleEvent(WiFiEvent_t event) {
    switch (event) {
    case SYSTEM_EVENT_STA_GOT_IP:
        Serial.println("[WiFi] connected");
        Serial.printf("[WiFi] IP: %s\r\n", WiFi.localIP().toString().c_str());

        _connecting = false;
        _retryInterval = RETRY_INTERVAL_MS;

        if (_onConnected) {
            _onConnected();
        }
        break;
    case SYSTEM_EVENT_STA_DISCONNECTED:
        Serial.println("[WiFi] disconnected");
        _connecting = false;
        if (_onDisconnected) {
            _onDisconnected();
        }
        break;
    }
}

bool WiFiManager::isConnected() {
    return WiFi.status() == WL_CONNECTED;
}

void WiFiManager::onConnected(std::function<void()> callback) {
    _onConnected = callback;
}

void WiFiManager::onDisconnected(std::function<void()> callback) {
    _onDisconnected = callback;
}

bool WiFiManager::triggerScan() {
    if (_scanInProgress) {
        return false;
    }

    int16_t status = WiFi.scanComplete();
    if (status == WIFI_SCAN_RUNNING) {
        _scanInProgress = true;
        return false;
    }

    if (status >= 0 || status == WIFI_SCAN_FAILED) {
        WiFi.scanDelete();
    }

    int16_t result = WiFi.scanNetworks(true, true);
    if (result == WIFI_SCAN_FAILED) {
        return false;
    }

    _scanInProgress = true;
    return true;
}

bool WiFiManager::scanInProgress() const {
    return _scanInProgress;
}

uint32_t WiFiManager::scanResultVersion() const {
    return _scanResultVersion;
}

const std::vector<WiFiScanNetwork>& WiFiManager::scanResults() const {
    return _scanResults;
}

void WiFiManager::connectStation() {
    if (_configuration == nullptr) {
        return;
    }

    if (!hasCompleteWifiConfig()) {
        return;
    }

    WifiPreference wifiPreference = _configuration->wifi();

    _connecting = true;
    _connectStart = millis();
    WiFi.begin(wifiPreference.ssid.c_str(), wifiPreference.password.c_str());
}

bool WiFiManager::hasCompleteWifiConfig() const {
    if (_configuration == nullptr) {
        return false;
    }

    WifiPreference wifiPreference = _configuration->wifi();
    return !wifiPreference.ssid.empty() &&
        !wifiPreference.password.empty();
}

void WiFiManager::updateScanResults() {
    if (!_scanInProgress) {
        return;
    }

    int16_t status = WiFi.scanComplete();
    if (status == WIFI_SCAN_RUNNING) {
        return;
    }

    _scanInProgress = false;
    _scanResults.clear();

    if (status <= 0) {
        WiFi.scanDelete();
        _scanResultVersion++;
        return;
    }

    _scanResults.reserve(static_cast<size_t>(status));
    for (int16_t i = 0; i < status; ++i) {
        WiFiScanNetwork network;
        network.ssid = WiFi.SSID(i).c_str();
        network.bssid = WiFi.BSSIDstr(i).c_str();
        network.rssi = WiFi.RSSI(i);
        network.channel = WiFi.channel(i);
        network.authMode = static_cast<wifi_auth_mode_t>(WiFi.encryptionType(i));
        network.hidden = network.ssid.empty();
        _scanResults.push_back(network);
    }

    std::sort(_scanResults.begin(), _scanResults.end(), [](const WiFiScanNetwork& left, const WiFiScanNetwork& right) {
        return left.rssi > right.rssi;
        });

    WiFi.scanDelete();
    _scanResultVersion++;
}
