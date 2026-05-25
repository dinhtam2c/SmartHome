#include "WiFiManager.h"

#include <Arduino.h>

#include <algorithm>

void WiFiManager::begin(Configuration& configuration) {
    _configuration = &configuration;
    _wifiPreference = _configuration->wifi();
    _scanMutex = xSemaphoreCreateMutex();
    _networkEventQueue = xQueueCreate(8, sizeof(NetworkEvent));

    WiFi.onEvent([this](WiFiEvent_t event, WiFiEventInfo_t info) { handleEvent(event, info); });
    // WiFiSTA enables automatic reconnect by default. This manager owns the
    // retry policy, so leaving it enabled would create overlapping attempts.
    WiFi.setAutoReconnect(false);
    WiFi.mode(WIFI_STA);
    _reconnectRequested.store(true);

    xTaskCreate(loopTask, "WiFiLoop", 4096, this, 1, NULL);
}

void WiFiManager::requestReconnect() {
    // Configuration is written by the provisioning task. Let WiFiLoop read it
    // and touch the Wi-Fi driver from one task only.
    _reconnectRequested.store(true);
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

    processNetworkEvents(now);
    updateScanResults();

    if (_reconnectRequested.exchange(false)) {
        bool hadActiveAttempt = _connecting || WiFi.status() == WL_CONNECTED;
        _wifiPreference = _configuration->wifi();
        _connecting = false;
        _restartPending = true;
        _retryInterval = INITIAL_RETRY_INTERVAL_MS;
        _nextTryAt = 0;

        if (hadActiveAttempt) {
            WiFi.disconnect(false, false);
            _restartAt = now + RECONNECT_SETTLE_MS;
        } else {
            _restartAt = now;
        }
    }

    // Do not run reconnect attempts until Wi-Fi credentials are configured.
    if (!hasCompleteWifiConfig()) {
        _connecting = false;
        _restartPending = false;
        _nextTryAt = 0;
        return;
    }

    if (isConnected()) {
        return;
    }

    if (_restartPending) {
        // A scan started by the captive portal may still be finishing when the
        // user submits credentials. Do not start STA association concurrently.
        if (scanInProgress()) {
            return;
        }
        if (deadlineReached(now, _restartAt)) {
            _restartPending = false;
            connectStation();
        }
        return;
    }

    if (_connecting && now - _connectStart >= CONNECT_TIMEOUT_MS) {
        Serial.println("[WiFi] connection attempt timed out");
        WiFi.disconnect(false, false);
        _connecting = false;
        scheduleRetry(now);
        return;
    }

    if (!_connecting && (_nextTryAt == 0 || deadlineReached(now, _nextTryAt))) {
        Serial.println("[WiFi] reconnecting...");
        connectStation();
    }
}

void WiFiManager::waitForConnection() {
    Serial.println("[WiFi] waiting for connection...");
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print('.');
    }
}

void WiFiManager::handleEvent(WiFiEvent_t event, WiFiEventInfo_t info) {
    switch (event) {
    case ARDUINO_EVENT_WIFI_STA_GOT_IP:
        if (_networkEventQueue != nullptr) {
            NetworkEvent networkEvent{ true, 0 };
            xQueueSend(_networkEventQueue, &networkEvent, 0);
        }
        break;
    case ARDUINO_EVENT_WIFI_STA_DISCONNECTED:
        if (_networkEventQueue != nullptr) {
            NetworkEvent networkEvent{
                false,
                static_cast<uint8_t>(info.wifi_sta_disconnected.reason)
            };
            xQueueSend(_networkEventQueue, &networkEvent, 0);
        }
        break;
    default:
        break;
    }
}

void WiFiManager::processNetworkEvents(unsigned long now) {
    // Arduino-ESP32 invokes callbacks from its own FreeRTOS task. Preserve the
    // event order in a queue and mutate connection state only from WiFiLoop.
    if (_networkEventQueue == nullptr) {
        return;
    }

    NetworkEvent networkEvent{};
    while (xQueueReceive(_networkEventQueue, &networkEvent, 0) == pdTRUE) {
        _connectionEpoch.fetch_add(1);

        if (networkEvent.connected) {
            Serial.println("[WiFi] connected");
            Serial.printf("[WiFi] IP: %s\r\n", WiFi.localIP().toString().c_str());

            _connecting = false;
            _restartPending = false;
            _nextTryAt = 0;
            _retryInterval = INITIAL_RETRY_INTERVAL_MS;

            bool notifyConnected = !_wasConnected;
            _wasConnected = true;
            if (notifyConnected && _onConnected) {
                _onConnected();
            }
            continue;
        }

        _connecting = false;
        bool shouldLog = _wasConnected ||
            (now - _lastDisconnectLog >= DISCONNECT_LOG_COOLDOWN_MS);
        if (shouldLog) {
            Serial.printf("[WiFi] disconnected (reason=%u)\r\n",
                static_cast<unsigned>(networkEvent.disconnectReason));
            _lastDisconnectLog = now;
        }

        if (_wasConnected && _onDisconnected) {
            _onDisconnected();
        }
        _wasConnected = false;

        if (!_restartPending && hasCompleteWifiConfig()) {
            scheduleRetry(now);
        }
    }
}

bool WiFiManager::isConnected() {
    return WiFi.status() == WL_CONNECTED;
}

uint32_t WiFiManager::connectionEpoch() const {
    return _connectionEpoch.load();
}

void WiFiManager::onConnected(std::function<void()> callback) {
    _onConnected = callback;
}

void WiFiManager::onDisconnected(std::function<void()> callback) {
    _onDisconnected = callback;
}

bool WiFiManager::triggerScan() {
    if (_scanMutex == nullptr ||
        xSemaphoreTake(_scanMutex, pdMS_TO_TICKS(100)) != pdTRUE) {
        return false;
    }

    if (_scanInProgress) {
        xSemaphoreGive(_scanMutex);
        return false;
    }

    int16_t status = WiFi.scanComplete();
    if (status == WIFI_SCAN_RUNNING) {
        _scanInProgress = true;
        xSemaphoreGive(_scanMutex);
        return false;
    }

    if (status >= 0 || status == WIFI_SCAN_FAILED) {
        WiFi.scanDelete();
    }

    int16_t result = WiFi.scanNetworks(true, true);
    if (result == WIFI_SCAN_FAILED) {
        xSemaphoreGive(_scanMutex);
        return false;
    }

    _scanInProgress = true;
    xSemaphoreGive(_scanMutex);
    return true;
}

bool WiFiManager::scanInProgress() const {
    if (_scanMutex == nullptr ||
        xSemaphoreTake(_scanMutex, pdMS_TO_TICKS(100)) != pdTRUE) {
        return false;
    }
    bool result = _scanInProgress;
    xSemaphoreGive(_scanMutex);
    return result;
}

uint32_t WiFiManager::scanResultVersion() const {
    if (_scanMutex == nullptr ||
        xSemaphoreTake(_scanMutex, pdMS_TO_TICKS(100)) != pdTRUE) {
        return 0;
    }
    uint32_t result = _scanResultVersion;
    xSemaphoreGive(_scanMutex);
    return result;
}

std::vector<WiFiScanNetwork> WiFiManager::scanResults() const {
    if (_scanMutex == nullptr ||
        xSemaphoreTake(_scanMutex, pdMS_TO_TICKS(100)) != pdTRUE) {
        return {};
    }
    std::vector<WiFiScanNetwork> result = _scanResults;
    xSemaphoreGive(_scanMutex);
    return result;
}

void WiFiManager::connectStation() {
    if (_configuration == nullptr) {
        return;
    }

    if (!hasCompleteWifiConfig()) {
        return;
    }

    _connecting = true;
    _connectStart = millis();
    _nextTryAt = 0;
    wl_status_t status = WiFi.begin(
        _wifiPreference.ssid.c_str(),
        _wifiPreference.password.empty() ? nullptr : _wifiPreference.password.c_str());
    if (status == WL_CONNECT_FAILED) {
        _connecting = false;
        scheduleRetry(_connectStart);
    }
}

void WiFiManager::scheduleRetry(unsigned long now) {
    if (_restartPending) {
        return;
    }

    if (_nextTryAt != 0 && !deadlineReached(now, _nextTryAt)) {
        return;
    }

    Serial.printf("[WiFi] next attempt in %lu ms\r\n", _retryInterval);
    _nextTryAt = now + _retryInterval;
    _retryInterval = std::min(_retryInterval * 2, MAX_RETRY_INTERVAL_MS);
}

bool WiFiManager::hasCompleteWifiConfig() const {
    return !_wifiPreference.ssid.empty();
}

void WiFiManager::updateScanResults() {
    if (_scanMutex == nullptr ||
        xSemaphoreTake(_scanMutex, pdMS_TO_TICKS(100)) != pdTRUE) {
        return;
    }

    if (!_scanInProgress) {
        xSemaphoreGive(_scanMutex);
        return;
    }

    int16_t status = WiFi.scanComplete();
    if (status == WIFI_SCAN_RUNNING) {
        xSemaphoreGive(_scanMutex);
        return;
    }

    _scanInProgress = false;
    _scanResults.clear();

    if (status <= 0) {
        WiFi.scanDelete();
        _scanResultVersion++;
        xSemaphoreGive(_scanMutex);
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
    xSemaphoreGive(_scanMutex);
}

bool WiFiManager::deadlineReached(unsigned long now, unsigned long deadline) {
    return static_cast<int32_t>(now - deadline) >= 0;
}
