#pragma once

#include <Arduino.h>
#include <ArduinoJson.h>
#include <Preferences.h>
#include <DHT_U.h>

#include <vector>

#include "device/GatewayTransport.h"
#include "common/device_message.h"

#include "device/config.h"

#define PREFS_NAMESPACE             "device"
#define PREFS_DEVICE_ID             "deviceId"
#define PREFS_TEMP_SENSOR_ID        "tempSensorId"
#define PREFS_HUM_SENSOR_ID         "humSensorId"
#define PREFS_LED_ID                "ledId"

#define LED_PIN 2
#define DHT22_PIN 32

struct SensorInfo {
    std::string id;
    std::string name;
    std::string type;
    std::string unit;
    float min;
    float max;
    float accuracy;
};

struct ActuatorInfo {
    std::string id;
    std::string name;
    std::string type;
    std::vector<std::string> states;
    std::vector<std::string> commands;
};

struct LED {
    ActuatorInfo info;
    bool power;
};

class Device {
public:
    Device(IGatewayTransport* transport);
    void begin();
    void loop();

    void loadPreferences();
    const std::string& getDeviceId();
    const std::string& getIdentifier();
    void handleGatewayMessage(DeviceMessageType messageType, const std::string& payload);

private:
    Preferences _prefs;

    std::string _identifier;
    std::string _deviceId;
    unsigned long _lastMsgTime;
    int _dataInterval;
    int _stateInterval;
    DHT _dht22;

    IGatewayTransport* _transport;

    SensorInfo _tempSensor;
    SensorInfo _humSensor;
    LED _led;

    void initCapabilities();

    void requestProvision();
    void handleProvisionResponse(const std::string& payload);

    void handleCommand(const std::string& payload);

    void readSensors(float* temp, float* hum);
    void buildAndSendData(float temp, float hum);

    void sendActuatorStates();
};
