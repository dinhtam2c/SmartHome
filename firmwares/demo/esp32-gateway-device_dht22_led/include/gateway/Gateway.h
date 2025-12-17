#pragma once

#include <Arduino.h>
#include <Preferences.h>
#include <ArduinoJson.h>

#include "gateway/WiFiManager.h"
#include "gateway/MqttManager.h"
#include "gateway/DeviceManager.h"

#include "common/device_message.h"

#include "gateway/config.h"

#define PREFS_NAMESPACE     "gateway"
#define PREFS_GATEWAY_ID    "gatewayId"

class Gateway {
public:
    void begin();
    void loop();

    void handleDeviceConnect(const std::string& deviceId);
    void handleDeviceMessage(const TransportMessage& message);

private:
    WiFiManager _wifi;
    MqttManager _mqtt;
    DeviceManager _deviceManager;

    Preferences _prefs;

    std::string _mac;
    std::string _gatewayId;

    std::string _topicPrefix;

    void onWiFiConnected();
    void onWiFiDisconnected();

    void requestProvision();
    void handleProvisionResponse(const std::string& payload);

    void handleDeviceProvision(const std::string& identifier, const std::string& message);
    void handleDeviceProvisionResponse(const std::string& identifier, const std::string& payload);

    void handleDeviceData(const std::string& deviceId, const std::string& message);
    void handleDeviceActuatorsStates(const std::string& deviceId, const std::string& payload);

    void handleMqttConnect();
    void handleMqttMessage(const std::string& topic, const std::string& payload);

    void setupSubscriptions();
};
