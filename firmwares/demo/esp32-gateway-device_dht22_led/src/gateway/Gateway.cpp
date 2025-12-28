#include "gateway/Gateway.h"

#include <string>
#include <time.h>

#include "common/device_message.h"
#include "gateway/utils.h"

void Gateway::begin() {
    String mac = WiFi.macAddress();
    _mac = mac.c_str();

    _bootTime = millis();

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
    unsigned long currentTime = millis();

    // Send availability
    if (currentTime - _lastAvailabilitySendTime >= AVAILABILITY_SEND_INTERVAL * 1000) {
        sendAvailability();
        _lastAvailabilitySendTime = currentTime;
    }

    // Send full state
    if (currentTime - _lastStateSendTime >= STATE_SEND_INTERVAL * 1000) {
        sendGatewayState();
        _lastStateSendTime = currentTime;
    }

    // Send device availability
    if (currentTime - _lastDeviceAvailabilitySendTime >= DEVICE_AVAILABILITY_SEND_INTERVAL * 1000) {
        sendAllDeviceAvailability();
        _lastDeviceAvailabilitySendTime = currentTime;
    }
}

void Gateway::setupSubscriptions() {
    _mqtt.subscribe(_topicPrefix + "/provision/response", 1);
    _mqtt.subscribe(_topicPrefix + "/devices/+/provision/response", 1);
    _mqtt.subscribe(_topicPrefix + "/devices/+/command", 2);
    _mqtt.subscribe(_topicPrefix + "/rule", 1);
}

void Gateway::handleMqttConnect() {
    sendAvailability();
    sendGatewayState();
    _lastAvailabilitySendTime = millis();
    _lastStateSendTime = millis();

    // Send availability for all connected devices
    sendAllDeviceAvailability();
    _lastDeviceAvailabilitySendTime = millis();
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
    case DEVICE_SYSTEM_STATE:
        handleDeviceSystemState(deviceId, payload);
        break;
    case DEVICE_ACTUATORS_STATES:
        handleDeviceActuatorsStates(deviceId, payload);
        break;
    default:
        Serial.println("Unknown message type");
        Serial.printf("messageType: %d\r\n", messageType);
        break;
    }
}

void Gateway::handleDeviceActuatorsStates(const std::string& deviceId, const std::string& payload) {
    std::string topic = _topicPrefix + "/devices/" + deviceId + "/states/actuators";
    _mqtt.publish(topic, payload, 1, true);
}

void Gateway::handleDeviceSystemState(const std::string& deviceId, const std::string& payload) {
    std::string topic = _topicPrefix + "/devices/" + deviceId + "/states/system";
    _mqtt.publish(topic, payload, 1, false);
}

void Gateway::sendDeviceAvailability(const std::string& deviceId) {
    std::string topic = _topicPrefix + "/devices/" + deviceId + "/availability";
    _mqtt.publish(topic, "{\"state\":\"Online\"}", 1, true);
}

void Gateway::sendGatewayState() {
    if (!_mqtt.isConnected() || _gatewayId.length() == 0) {
        return;
    }

    unsigned long uptime = (millis() - _bootTime) / 1000; // Convert to seconds
    int deviceCount = _deviceManager.getConnectedDeviceCount();

    JsonDocument doc;
    doc["uptime"] = uptime;
    doc["deviceCount"] = deviceCount;

    std::string payload;
    serializeJson(doc, payload);

    std::string stateTopic = _topicPrefix + "/state";
    _mqtt.publish(stateTopic, payload, 1, false);
}

void Gateway::sendAvailability() {
    if (!_mqtt.isConnected() || _gatewayId.length() == 0) {
        return;
    }

    std::string availTopic = _topicPrefix + "/availability";
    _mqtt.publish(availTopic, "{\"state\":\"Online\"}", 1, true);
}

void Gateway::sendAllDeviceAvailability() {
    if (!_mqtt.isConnected() || _gatewayId.length() == 0) {
        return;
    }

    auto deviceIds = _deviceManager.getConnectedDeviceIds();
    for (const auto& deviceId : deviceIds) {
        sendDeviceAvailability(deviceId);
    }
}
