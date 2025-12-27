#include "device/Device.h"

void Device::handleCommand(const std::string& payload) {
    JsonDocument doc;
    deserializeJson(doc, payload);

    const std::string& actuatorId = doc["actuatorId"];
    const std::string& command = doc["command"];

    if (actuatorId != _led.info.id) {
        Serial.println("Received command for an unknown actuator");
        return;
    }

    Serial.printf("[Device] Received command: %s\r\n", command.c_str());

    if (command == "TurnOn") {
        _led.power = true;
        digitalWrite(LED_PIN, HIGH);
    } else if (command == "TurnOff") {
        _led.power = false;
        digitalWrite(LED_PIN, LOW);
    } else {
        Serial.printf("Unknown command: %s\r\n", command.c_str());
        return;
    }

    sendActuatorStates();
}

void Device::sendActuatorStates() {
    Serial.println("Sending actuators states");

    JsonDocument doc;
    JsonArray arr = doc.to<JsonArray>();

    JsonObject act = arr.add<JsonObject>();
    act["actuatorId"] = _led.info.id;

    JsonObject states = act["states"].to<JsonObject>();
    states["Power"] = _led.power ? "On" : "Off";

    std::string payload;
    serializeJson(doc, payload);

    TransportMessage message = { _deviceId, DEVICE_ACTUATORS_STATES, payload };
    _transport->send(message);
}

void Device::sendSystemState() {
    if (_deviceId.length() == 0) {
        return;
    }

    Serial.println("Sending system state");

    unsigned long uptime = (millis() - _bootTime) / 1000; // Convert to seconds

    JsonDocument doc;
    doc["uptime"] = uptime;

    std::string payload;
    serializeJson(doc, payload);

    TransportMessage message = { _deviceId, DEVICE_SYSTEM_STATE, payload };
    _transport->send(message);
}
