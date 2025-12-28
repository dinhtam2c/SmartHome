#include "gateway/WiFiManager.h"

#include <Arduino.h>

#include "gateway/config.h"

void WiFiManager::begin(const char* ssid, const char* password,
    unsigned long initialInterval, unsigned long maxInterval, unsigned long intervalStep) {
    _ssid = ssid;
    _password = password;

    _retryInterval = initialInterval;
    _initialInterval = initialInterval;
    _maxInterval = maxInterval;
    _intervalStep = intervalStep;

    WiFi.onEvent([this](WiFiEvent_t event, WiFiEventInfo_t){ handleEvent(event); });

    WiFi.begin(_ssid, _password);
    xTaskCreate(loopTask, "WiFiLoop", 2048, this, 1, NULL);
}

void WiFiManager::begin() {
    begin(SSID, PASSWORD);
}

void WiFiManager::loopTask(void* self) {
    while (true) {
        ((WiFiManager*)self)->loop();
        // TODO: check stack size
        vTaskDelay(500 / portTICK_PERIOD_MS);
    }
}

void WiFiManager::loop() {
    unsigned long now = millis();

    if (!isConnected() && WiFi.status() != WL_IDLE_STATUS && now - _lastTry >= _retryInterval) {
        Serial.println("Reconnecting WiFi...");
        WiFi.begin(_ssid, _password);

        _lastTry = now;
        _retryInterval = min(_retryInterval + _intervalStep, _maxInterval);
    }
}

void WiFiManager::waitForConnection() {
    Serial.println("Waiting for WiFi connection...");
    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print('.');
    }
}

void WiFiManager::handleEvent(WiFiEvent_t event) {
    switch (event)
    {
    case SYSTEM_EVENT_STA_GOT_IP:
        Serial.println("Wi-Fi connected");
        Serial.printf("IP: %s\r\n", WiFi.localIP().toString());

        _retryInterval = _initialInterval;
        
        if (_onConnected)
            _onConnected();
        break;
    case SYSTEM_EVENT_STA_DISCONNECTED:
        Serial.println("Wi-Fi disconnected");
        if (_onDisconnected)
            _onDisconnected();
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
