#include "device/Device.h"

#include <Arduino.h>
#include <Preferences.h>

#include <DHT_U.h>

#include "common/device_message.h"

DHT dht22(DHT22_PIN, DHT22);

Device::Device(IGatewayTransport* transport) {
    _transport = transport;
}

void Device::loadPreferences() {
    _identifier = IDENTIFIER;
    _prefs.begin(PREFS_NAMESPACE);
    String deviceId = _prefs.getString(PREFS_DEVICE_ID, "");
    String tempSensorId = _prefs.getString(PREFS_TEMP_SENSOR_ID, "");
    String humSensorId = _prefs.getString(PREFS_HUM_SENSOR_ID, "");
    String ledId = _prefs.getString(PREFS_LED_ID, "");
    _prefs.end();

    _deviceId = deviceId.c_str();
    _tempSensor.id = tempSensorId.c_str();
    _humSensor.id = humSensorId.c_str();
    _led.id = ledId.c_str();
}

void Device::initCapabilities() {
    _tempSensor.name = "DHT22_Temp";
    _tempSensor.type = "Temperature";
    _tempSensor.unit = "C";
    _tempSensor.min = -40;
    _tempSensor.max = 80;
    _tempSensor.accuracy = 0.5;

    _humSensor.name = "DHT22_Hum";
    _humSensor.type = "Humidity";
    _humSensor.unit = "%";
    _humSensor.min = 0;
    _humSensor.max = 100;
    _humSensor.accuracy = 5;


    _led.name = "LED";
    _led.type = "Light";

    _led.states.push_back("Power");

    _led.commands.push_back("TurnOn");
    _led.commands.push_back("TurnOff");
}

void Device::begin(int interval) {
    _interval = interval;

    initCapabilities();

    /* Connect to gateway here */

    if (_deviceId.length() == 0) {
        requestProvision();
        return;
    }

    pinMode(LED_PIN, OUTPUT);
    digitalWrite(LED_PIN, HIGH);

    dht22.begin();
}

void Device::loop() {
    time_t now = time(NULL);

    if (now - _lastMsgTime < _interval)
        return;

    _lastMsgTime = now;

    float temp, hum;
    readSensors(&temp, &hum);

    buildAndSendData(temp, hum);
}

const std::string& Device::getDeviceId() {
    return _deviceId;
}

const std::string& Device::getIdentifier() {
    return _identifier;
}

void Device::handleGatewayMessage(DeviceMessageType messageType, const std::string& payload) {
    switch (messageType) {
    case DEVICE_PROVISION_RESPONSE:
        handleProvisionResponse(payload);
        break;
    case DEVICE_COMMAND: {
        handleCommand(payload);
        break;
    }
    default:
        break;
    }
}

void Device::handleCommand(const std::string& payload) {
    JsonDocument doc;
    deserializeJson(doc, payload);

    const std::string& actuatorId = doc["actuatorId"];
    const std::string& command = doc["command"];

    if (actuatorId != _led.id) {
        Serial.println("Received command for an unknown actuator");
        return;
    }

    Serial.printf("[Device] Received command: %s\r\n", command.c_str());

    if (command == "TurnOn") {
        digitalWrite(LED_PIN, HIGH);
    } else if (command == "TurnOff") {
        digitalWrite(LED_PIN, LOW);
    }
}

void Device::readSensors(float* temp, float* hum) {
    *temp = dht22.readTemperature();
    *hum = dht22.readHumidity();
    Serial.printf("[Device] Temp: %.1f  Hum: %.1f\n", *temp, *hum);
}

void Device::buildAndSendData(float temp, float hum) {
    long now = time(nullptr);

    JsonDocument doc;
    doc["deviceId"] = _deviceId;
    doc["timestamp"] = now;
    doc["priority"] = "LOW";

    JsonArray dataArray = doc["data"].to<JsonArray>();
    addSensorData(dataArray, _tempSensor, temp);
    addSensorData(dataArray, _humSensor, hum);

    TransportMessage message;
    serializeJson(doc, message.payload);
    message.deviceId = _deviceId;
    message.messageType = DEVICE_DATA;

    _transport->send(message);
}

void Device::addSensorData(JsonArray& arr, const SensorInfo& info, float value) {
    JsonObject obj = arr.add<JsonObject>();
    obj["sensorId"] = info.id;
    obj["value"] = value;
}
