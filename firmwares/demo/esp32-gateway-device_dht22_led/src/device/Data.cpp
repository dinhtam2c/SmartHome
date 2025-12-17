#include "device/Device.h"

static void addSensorData(JsonArray& arr, const SensorInfo& info, float value) {
    JsonObject obj = arr.add<JsonObject>();
    obj["sensorId"] = info.id;
    obj["value"] = value;
}

void Device::readSensors(float* temp, float* hum) {
    *temp = _dht22.readTemperature();
    *hum = _dht22.readHumidity();
    Serial.printf("[Device] Temp: %.1f  Hum: %.1f\n", *temp, *hum);
}

void Device::buildAndSendData(float temp, float hum) {
    Serial.println("[Device] Sending sensor data");

    long now = time(nullptr);

    JsonDocument doc;
    doc["deviceId"] = _deviceId;
    doc["timestamp"] = now;
    doc["priority"] = "LOW";

    JsonArray dataArray = doc["data"].to<JsonArray>();
    addSensorData(dataArray, _tempSensor, temp);
    addSensorData(dataArray, _humSensor, hum);

    std::string payload;
    serializeJson(doc, payload);

    TransportMessage message = { _deviceId, DEVICE_DATA, payload };
    _transport->send(message);
}
