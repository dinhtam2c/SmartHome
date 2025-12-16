#include "gateway/Gateway.h"

#include <string>
#include <time.h>

#include "common/device_message.h"
#include "gateway/utils.h"

void Gateway::begin() {
    String mac = WiFi.macAddress();
    _mac = mac.c_str();

    _prefs.begin(PREFS_NAMESPACE);
    String gatewayId = _prefs.getString(PREFS_GATEWAY_ID, "");
    _gatewayId = gatewayId.c_str();
    _prefs.end();

    _wifi.begin(SSID, PASSWORD);
    _wifi.waitForConnection();
    syncTimeAndWait();

    if (_gatewayId.length() == 0) {
        requestProvision();
        return;
    }

    _topicPrefix = "home/gateways/" + _gatewayId;
    setupSubscriptions();

    std::string availTopic = _topicPrefix + "/availability";
    Will will = { availTopic, "{\"state\":\"Offline\"}", 1, true };

    _mqtt.onConnected([this]() { handleMqttConnect(); });
    _mqtt.onMessage([this](const std::string& topic, const std::string& payload) { handleMqttMessage(topic, payload); });
    _mqtt.begin(&_wifi, MQTT_HOST, MQTT_PORT, _gatewayId, false, &will);
    _mqtt.waitForConnection(0);

    _deviceManager.begin(this);
}

void Gateway::loop() {
}

void Gateway::setupSubscriptions() {
    _mqtt.subscribe(_topicPrefix + "/provision/response", 1);
    _mqtt.subscribe(_topicPrefix + "/devices/+/provision/response", 1);
    _mqtt.subscribe(_topicPrefix + "/devices/+/command", 2);
    _mqtt.subscribe(_topicPrefix + "/rule", 1);
}

void Gateway::handleMqttConnect() {
    std::string availTopic = _topicPrefix + "/availability";
    _mqtt.publish(availTopic, "{\"state\":\"Online\"}", 1, true);
}

void Gateway::handleMqttMessage(const std::string& topic, const std::string& payload) {
    Serial.printf("Received message from topic: %s\r\n", topic.c_str());
    Serial.printf("Payload: %s\r\n", payload.c_str());

    auto tokens = split(topic, '/');
    if (tokens.size() < 3) {
        Serial.println("Invalid topic format");
        return;
    }

    if (tokens.size() == 5 && tokens[3] == "provision" && tokens[4] == "response") {
        handleProvisionResponse(payload);
        return;
    }

    if (tokens.size() > 5 && tokens[3] == "devices") {
        if (tokens[4] == "whitelist") {
            return;
        }

        const std::string& deviceId = tokens[4];

        if (tokens.size() == 7 && tokens[5] == "provision" && tokens[6] == "response") {
            handleDeviceProvisionResponse(deviceId, payload);
            return;
        }

        if (tokens.size() == 6 && tokens[5] == "command") {
            _deviceManager.routeCommand(deviceId, payload);
            return;
        }
    }

    if (tokens.size() == 4 && tokens[3] == "rule") {
        return;
    }
}

void Gateway::handleDeviceMessage(const TransportMessage& message) {
    const std::string& deviceId = message.deviceId;
    DeviceMessageType messageType = message.messageType;
    const std::string& payload = message.payload;

    Serial.printf("Received message from device: %s\r\n", deviceId.c_str());
    Serial.printf("Type: %d\r\n", messageType);

    switch (messageType) {
    case DEVICE_PROVISION:
        handleDeviceProvision(deviceId, payload);
        break;
    case DEVICE_DATA:
        handleDeviceData(deviceId, payload);
        break;
    default:
        Serial.println("Unknown message type");
        Serial.printf("messageType: %d\r\n", messageType);
        break;
    }
}

void Gateway::handleDeviceConnect(const std::string& deviceId) {
    // Send online message
    // TODO: send periodically + timeout on server
    std::string topic = _topicPrefix + "/devices/" + deviceId + "/availability";
    _mqtt.publish(topic, "{\"state\":\"Online\"}", 1, true);
}
