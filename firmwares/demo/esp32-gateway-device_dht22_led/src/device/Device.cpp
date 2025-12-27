#include "device/Device.h"

#include <Arduino.h>
#include <Preferences.h>

#include "common/device_message.h"

Device::Device(IGatewayTransport* transport) : _dht22(DHT22_PIN, DHT22) {
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
    _led.info.id = ledId.c_str();
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


    _led.info.name = "LED";
    _led.info.type = "Light";

    _led.info.states.push_back("Power");

    _led.info.commands.push_back("TurnOn");
    _led.info.commands.push_back("TurnOff");
}

void Device::begin() {
    _bootTime = millis();
    _dataInterval = DATA_INTERVAL;
    _stateInterval = STATE_INTERVAL;

    initCapabilities();

    /* Connect to gateway here */

    if (_deviceId.length() == 0) {
        requestProvision();
        return;
    }

    pinMode(LED_PIN, OUTPUT);
    _dht22.begin();

    digitalWrite(LED_PIN, HIGH);
    _led.power = true;
    sendActuatorStates();
}

void Device::loop() {
    unsigned long now = millis();

    if (now - _lastMsgTime >= _dataInterval * 1000) {
        _lastMsgTime = now;

        float temp, hum;
        readSensors(&temp, &hum);
        buildAndSendData(temp, hum);
    }
    
    if (now - _lastStateSendTime >= _stateInterval * 1000) {
        _lastStateSendTime = now;
        sendSystemState();
    }
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
