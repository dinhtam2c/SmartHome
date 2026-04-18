#include <Arduino.h>
#include <ArduinoJson.h>
#include <DHT.h>

#include <cmath>

#include "Capability.h"
#include "Device.h"

#define DHT_PIN 32
#define DHT_TYPE DHT22
#define RESET_PIN 0

DHT dht(DHT_PIN, DHT_TYPE);

std::string apSsid = "TempHumSensor";
std::string apPassword = "toidepchai";
std::string deviceName = "TempHumSensor";
std::string firmwareVersion = "1.0.0";

constexpr uint32_t DEFAULT_REPORT_INTERVAL_SEC = 30;
constexpr uint32_t MIN_REPORT_INTERVAL_SEC = 1;
constexpr uint32_t MAX_REPORT_INTERVAL_SEC = 3600;

uint32_t reportIntervalSec = DEFAULT_REPORT_INTERVAL_SEC;
unsigned long lastReportAtMs = 0;

float latestTemperatureC = NAN;
float latestHumidityPct = NAN;
bool sensorReadOk = false;

EndpointDefinition mainEndpoint = { "dht22_1", "DHT22 Sensor" };

void readDht22() {
    float humidity = dht.readHumidity();
    float temperature = dht.readTemperature();

    if (std::isnan(humidity) || std::isnan(temperature)) {
        sensorReadOk = false;
        Serial.println("[DHT22] Read failed");
        return;
    }

    latestHumidityPct = humidity;
    latestTemperatureC = temperature;
    sensorReadOk = true;

    Serial.print("[DHT22] Temp(C): ");
    Serial.print(latestTemperatureC, 1);
    Serial.print(" | Humidity(%): ");
    Serial.println(latestHumidityPct, 1);
}

CapabilityDefinition buildTemperatureSensorCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "sensor.temperature";
    capability.supportedOperations = {};
    capability.endpointId = "dht22_1";
    capability.handleCommand = nullptr;
    capability.getStateJson = []() {
        JsonDocument state;
        if (sensorReadOk) {
            state["value"] = latestTemperatureC;
        } else {
            state["value"] = nullptr;
        }

        std::string stateJson;
        serializeJson(state, stateJson);
        return stateJson;
        };

    return capability;
}

CapabilityDefinition buildHumiditySensorCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "sensor.humidity";
    capability.supportedOperations = {};
    capability.endpointId = "dht22_1";
    capability.handleCommand = nullptr;
    capability.getStateJson = []() {
        JsonDocument state;
        if (sensorReadOk) {
            state["value"] = latestHumidityPct;
        } else {
            state["value"] = nullptr;
        }

        std::string stateJson;
        serializeJson(state, stateJson);
        return stateJson;
        };

    return capability;
}

CapabilityDefinition buildReportIntervalCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "sensor.interval";
    capability.supportedOperations = { "set" };
    capability.endpointId = "dht22_1";
    capability.getStateJson = []() {
        JsonDocument state;
        state["value"] = reportIntervalSec;

        std::string stateJson;
        serializeJson(state, stateJson);
        return stateJson;
        };
    capability.handleCommand = [](const std::string& operation,
        JsonVariantConst commandValue,
        std::string& outStateJson,
        std::string& outError) {
            if (operation != "set") {
                outError = "operation_not_supported";
                return false;
            }

            uint32_t newIntervalSec = commandValue["value"].as<uint32_t>();
            if (newIntervalSec < MIN_REPORT_INTERVAL_SEC ||
                newIntervalSec > MAX_REPORT_INTERVAL_SEC) {
                outError = "value_out_of_range";
                return true;
            }

            reportIntervalSec = newIntervalSec;

            JsonDocument state;
            state["value"] = reportIntervalSec;
            serializeJson(state, outStateJson);

            Serial.print("[Report] Interval set to ");
            Serial.print(reportIntervalSec);
            Serial.println(" s");
            return true;
        };

    return capability;
}

std::vector<EndpointDefinition> endpoints = { mainEndpoint };

std::vector<CapabilityDefinition> capabilities = {
    buildTemperatureSensorCapability(),
    buildHumiditySensorCapability(),
    buildReportIntervalCapability()
};

Device device(apSsid, apPassword, deviceName, firmwareVersion, endpoints, capabilities);

void setup() {
    Serial.begin(115200);
    dht.begin();
    readDht22();

    device.begin();
}

unsigned long resetPressStart = 0;
bool wasPressed = false;

void loop() {
    device.loop();

    unsigned long now = millis();
    if (now - lastReportAtMs >= reportIntervalSec * 1000UL) {
        readDht22();
        device.publishCapabilityStates(true);
        lastReportAtMs = now;
    }

    if (digitalRead(RESET_PIN) == LOW) {
        if (!wasPressed) {
            resetPressStart = millis();
            wasPressed = true;
        } else if (millis() - resetPressStart >= 5000) {
            Serial.println("Resetting configuration...");
            device.resetConfiguration();
            ESP.restart();
        }
    } else {
        wasPressed = false;
    }
}
