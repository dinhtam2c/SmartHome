#include "gateway/Gateway.h"

void Gateway::handleDeviceData(const std::string& deviceId, const std::string& message) {
    JsonDocument dataJson;
    deserializeJson(dataJson, message);
    std::string priority = dataJson["priority"];

    // TODO: priority

    JsonDocument doc;
    doc["gatewayId"] = _gatewayId;
    doc["timestamp"] = time(NULL);
    JsonArray dataArray = doc["data"].to<JsonArray>();
    dataArray.add(dataJson);

    std::string payload;
    serializeJson(doc, payload);

    std::string topic = _topicPrefix + "/data";

    _mqtt.publish(topic, payload, 0, false);
}
