#include <Arduino.h>
#include "Device.h"
#include <DHT.h>
#include <math.h>

#define RESET_PIN 0
#define DHT22_PIN 25
#define PIR_PIN 12
#define LDR_AO_PIN 34

std::string apSsid = "SensorHub-Setup";
std::string apPassword = "toidepchai";
std::string deviceName = "Sensor Hub";
std::string deviceCategory = "sensor";
std::string firmwareVersion = "1.0.0";

float temperature = NAN;
float humidity = NAN;
bool motionDetected = false;
int illuminanceRaw = 0;

EndpointDefinition mainEndpoint = { "main", "Main" };

std::vector<EndpointDefinition> endpoints = { mainEndpoint };

std::string jsonSensorState(float value, int precision) {
    if (isnan(value) || isinf(value)) {
        return "";
    }

    char buffer[32];
    snprintf(buffer, sizeof(buffer), "%.*f", precision, value);
    return std::string("{\"value\":") + buffer + "}";
}

CapabilityDefinition buildTemperatureCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "sensor.temperature";
    capability.endpointId = mainEndpoint.endpointId;
    capability.supportedOperations = {};
    capability.getStateJson = []() {
        return jsonSensorState(temperature, 1);
        };

    capability.handleCommand = nullptr;

    return capability;
}

CapabilityDefinition buildHumidityCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "sensor.humidity";
    capability.endpointId = mainEndpoint.endpointId;
    capability.supportedOperations = {};
    capability.getStateJson = []() {
        return jsonSensorState(humidity, 1);
        };

    capability.handleCommand = nullptr;

    return capability;
}

CapabilityDefinition buildMotionCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "sensor.motion";
    capability.endpointId = mainEndpoint.endpointId;
    capability.supportedOperations = {};
    capability.getStateJson = []() {
        return "{\"value\":" + std::string(motionDetected ? "true" : "false") + "}";
        };

    capability.handleCommand = nullptr;

    return capability;
}

CapabilityDefinition buildIlluminanceCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "sensor.illuminance";
    capability.endpointId = mainEndpoint.endpointId;
    capability.supportedOperations = {};
    capability.getStateJson = []() {
        return "{\"value\":" + std::to_string(illuminanceRaw) + "}";
        };

    capability.handleCommand = nullptr;

    return capability;
}

std::vector<CapabilityDefinition> capabilities = {
    buildTemperatureCapability(),
    buildHumidityCapability(),
    buildMotionCapability(),
    buildIlluminanceCapability()
};

Device device(apSsid, apPassword, deviceName, deviceCategory, firmwareVersion, endpoints, capabilities);

DHT dht(DHT22_PIN, DHT22);
unsigned long lastDht22Read = 0;
const unsigned long DHT22_READ_INTERVAL_MS = 2000;

void setup() {
    Serial.begin(115200);
    pinMode(RESET_PIN, INPUT_PULLUP);
    pinMode(PIR_PIN, INPUT);
    pinMode(LDR_AO_PIN, INPUT);
    analogReadResolution(12);
    analogSetPinAttenuation(LDR_AO_PIN, ADC_11db);

    dht.begin();
    device.begin();
}

#define RESET_HOLD_MS 5000
unsigned long resetPressStart = 0;
bool wasResetPressed = false;

void loop() {
    unsigned long now = millis();

    if (now - lastDht22Read >= DHT22_READ_INTERVAL_MS) {
        humidity = dht.readHumidity();
        temperature = dht.readTemperature();
        illuminanceRaw = analogRead(LDR_AO_PIN);

        lastDht22Read = now;
    }

    motionDetected = digitalRead(PIR_PIN) == HIGH;

    device.publishCapabilityStates();

    if (digitalRead(RESET_PIN) == LOW) {
        if (!wasResetPressed) {
            resetPressStart = now;
            wasResetPressed = true;
        } else if (now - resetPressStart >= RESET_HOLD_MS) {
            device.resetConfiguration();
            ESP.restart();
        }
    } else {
        wasResetPressed = false;
    }

    device.loop();
}
