#include "gateway/Gateway.h"

void Gateway::requestProvision() {
    _topicPrefix = "home/gateways/" + _mac + "/provision/";
    std::string responseTopic = _topicPrefix + "response";

    _mqtt.subscribe(responseTopic, 1);
    _mqtt.begin(&_wifi, MQTT_HOST, MQTT_PORT);
    _mqtt.waitForConnection(0);

    JsonDocument doc;
    doc["key"] = PROVISION_KEY;
    doc["name"] = NAME;
    doc["mac"] = _mac;
    doc["manufacturer"] = MANUFACTURER;
    doc["model"] = MODEL;
    doc["firmwareVersion"] = FIRMWARE_VERSION;
    doc["timestamp"] = time(NULL);

    std::string payload;
    serializeJson(doc, payload);

    std::string requestTopic = _topicPrefix + "request";

    Serial.println("Sending provision request");
    _mqtt.publish(requestTopic, payload, 1, false);

    /* Wait and retry */
    delay(300000);
    ESP.restart();
}

void Gateway::handleProvisionResponse(const std::string& payload) {
    JsonDocument doc;
    deserializeJson(doc, payload);

    const std::string& gatewayId = doc["gatewayId"];
    Serial.printf("Received uuid: %s\r\n", gatewayId.c_str());

    _prefs.begin(PREFS_NAMESPACE);
    if (gatewayId != "null") {
        Serial.printf("Storing uuid: %s\r\n", gatewayId.c_str());
        _gatewayId = _prefs.putString(PREFS_GATEWAY_ID, gatewayId.c_str());
    } else {
        Serial.println("Deleting uuid");
        _prefs.remove(PREFS_GATEWAY_ID);
    }
    _prefs.end();

    ESP.restart();
}

void Gateway::handleDeviceProvision(const std::string& identifier, const std::string& message) {
    JsonDocument request;
    deserializeJson(request, message);
    request["gatewayId"] = _gatewayId;

    std::string payload;
    serializeJson(request, payload);

    std::string topic = _topicPrefix + "/devices/" + identifier + "/provision/request";

    _mqtt.publish(topic, payload, 1, false);
}

void Gateway::handleDeviceProvisionResponse(const std::string& identifier, const std::string& payload) {
    _deviceManager.routeProvisionResponse(identifier, payload);
}
