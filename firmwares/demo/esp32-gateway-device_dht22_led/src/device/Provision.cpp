#include "device/Device.h"

static void addSensor(JsonArray& arr, const SensorInfo& info) {
    JsonObject obj = arr.add<JsonObject>();
    obj["name"] = info.name;
    obj["type"] = info.type;
    obj["unit"] = info.unit;
    obj["min"] = info.min;
    obj["max"] = info.max;
    obj["accuracy"] = info.accuracy;
}

static void addActuator(JsonArray& arr, const ActuatorInfo& info) {
    JsonObject obj = arr.add<JsonObject>();
    obj["name"] = info.name;
    obj["type"] = info.type;

    JsonArray states = obj["states"].to<JsonArray>();
    for (int i = 0; i < info.states.size(); i++) {
        states.add(info.states[i]);
    }

    JsonArray commands = obj["commands"].to<JsonArray>();
    for (int i = 0; i < info.commands.size(); i++) {
        commands.add(info.commands[i]);
    }
}

void Device::requestProvision() {
    Serial.println("[Device] Provisioning");

    JsonDocument doc;
    doc["key"] = "xyz";

    /* Metadata */
    doc["identifier"] = _identifier;
    doc["name"] = NAME;
    doc["manufacturer"] = MANUFACTURER;
    doc["model"] = MODEL;
    doc["firmwareVersion"] = FIRMWARE_VERSION;
    doc["timestamp"] = time(NULL);

    /* Sensors */
    JsonArray sensorArray = doc["sensors"].to<JsonArray>();
    addSensor(sensorArray, _tempSensor);
    addSensor(sensorArray, _humSensor);

    /* Actuator */
    JsonArray actuatorArray = doc["actuators"].to<JsonArray>();
    addActuator(actuatorArray, _led);

    TransportMessage message;
    serializeJson(doc, message.payload);
    message.deviceId = _identifier;
    message.messageType = DEVICE_PROVISION;

    _transport->send(message);

    delay(300000);
    ESP.restart();
}

void Device::handleProvisionResponse(const std::string& payload) {
    JsonDocument response;
    deserializeJson(response, payload);

    const std::string& identifier = response["deviceIdentifier"];

    if (identifier != _identifier && identifier != _deviceId) {
        Serial.println("[Device] Received wrong provision response message");
        return;
    }

    const std::string& deviceId = response["deviceId"];
    JsonArray sensorIds = response["sensorIds"];
    JsonArray actuatorIds = response["actuatorIds"];

    if (deviceId != "null") {
        if (sensorIds.size() < 2 || actuatorIds.size() < 1) {
            Serial.println("[Device] Provision array size invalid");
            return;
        }

        const std::string& tempSensorId = sensorIds[0];
        const std::string& humSensorId = sensorIds[1];

        const std::string& ledId = actuatorIds[0];

        Serial.printf("[Device] Received uuid: %s\r\n", deviceId.c_str());
        Serial.printf("[Device] Temp Sensor ID: %s\r\n", tempSensorId.c_str());
        Serial.printf("[Device] Hum Sensor ID: %s\r\n", humSensorId.c_str());
        Serial.printf("[Device] LED Actuator ID: %s\r\n", ledId.c_str());

        _prefs.begin(PREFS_NAMESPACE);
        _prefs.putString(PREFS_DEVICE_ID, deviceId.c_str());
        _prefs.putString(PREFS_TEMP_SENSOR_ID, tempSensorId.c_str());
        _prefs.putString(PREFS_HUM_SENSOR_ID, humSensorId.c_str());
        _prefs.putString(PREFS_LED_ID, ledId.c_str());
        _prefs.end();

        Serial.println("[Device] Provisioning completed. Restarting...");
    } else {
        Serial.println("[Device] Received null uuid. Deleting stored uuid.");
        _prefs.begin(PREFS_NAMESPACE);
        _prefs.remove(PREFS_DEVICE_ID);
        _prefs.remove(PREFS_TEMP_SENSOR_ID);
        _prefs.remove(PREFS_HUM_SENSOR_ID);
        _prefs.remove(PREFS_LED_ID);
        _prefs.end();
    }

    ESP.restart();
}
