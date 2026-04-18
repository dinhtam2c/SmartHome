#include <Arduino.h>

#include <freertos/FreeRTOS.h>
#include <freertos/queue.h>
#include <freertos/semphr.h>

#include "Capability.h"
#include "Device.h"

#define LED1_PIN LED_BUILTIN
#define LED2_PIN 32
#define RESET_PIN 0

#define LED2_PWM_CHANNEL 0
#define LED2_PWM_FREQUENCY_HZ 5000
#define LED2_PWM_RESOLUTION_BITS 8

#define TRANSITION_STEP_MS 10
#define RESET_HOLD_MS 5000
#define LED2_COMMAND_QUEUE_LENGTH 1

std::string apSsid = "Light-1";
std::string apPassword = "toidepchai";
std::string deviceName = "Light-1";
std::string firmwareVersion = "1.0.0";

bool led1Power = false;
bool led2Power = false;
int led2Brightness = 100;
int led2AppliedBrightness = 0;

struct Led2Command {
    bool power;
    int brightness;
    int transitionMs;
};

struct Led2StateSnapshot {
    bool power;
    int brightness;
    int appliedBrightness;
};

SemaphoreHandle_t led2StateMutex = nullptr;
QueueHandle_t led2CommandQueue = nullptr;

EndpointDefinition endpoint1 = { "led1", "LED 1" };
EndpointDefinition endpoint2 = { "led2", "LED 2" };

std::string boolStateJson(bool value) {
    return value ? "{\"value\":true}" : "{\"value\":false}";
}

int clampBrightness(int value) {
    return constrain(value, 0, 100);
}

int brightnessToDuty(int value) {
    return map(clampBrightness(value), 0, 100, 0, 255);
}

void writeLed2Brightness(int value) {
    ledcWrite(LED2_PWM_CHANNEL, brightnessToDuty(value));
}

Led2StateSnapshot getLed2StateSnapshot() {
    Led2StateSnapshot snapshot = { led2Power, led2Brightness, led2AppliedBrightness };

    if (led2StateMutex != nullptr && xSemaphoreTake(led2StateMutex, portMAX_DELAY) == pdTRUE) {
        snapshot.power = led2Power;
        snapshot.brightness = led2Brightness;
        snapshot.appliedBrightness = led2AppliedBrightness;
        xSemaphoreGive(led2StateMutex);
    }

    return snapshot;
}

void setLed2Power(bool power) {
    if (led2StateMutex != nullptr && xSemaphoreTake(led2StateMutex, portMAX_DELAY) == pdTRUE) {
        led2Power = power;
        xSemaphoreGive(led2StateMutex);
        return;
    }

    led2Power = power;
}

void setLed2Brightness(int brightness) {
    int boundedBrightness = clampBrightness(brightness);

    if (led2StateMutex != nullptr && xSemaphoreTake(led2StateMutex, portMAX_DELAY) == pdTRUE) {
        led2Brightness = boundedBrightness;
        xSemaphoreGive(led2StateMutex);
        return;
    }

    led2Brightness = boundedBrightness;
}

void setLed2AppliedBrightness(int brightness) {
    int boundedBrightness = clampBrightness(brightness);

    if (led2StateMutex != nullptr && xSemaphoreTake(led2StateMutex, portMAX_DELAY) == pdTRUE) {
        led2AppliedBrightness = boundedBrightness;
        xSemaphoreGive(led2StateMutex);
        return;
    }

    led2AppliedBrightness = boundedBrightness;
}

void enqueueLed2Command(int transitionMs) {
    if (led2CommandQueue == nullptr) {
        return;
    }

    Led2StateSnapshot snapshot = getLed2StateSnapshot();
    Led2Command command = {
        snapshot.power,
        clampBrightness(snapshot.brightness),
        transitionMs < 0 ? 0 : transitionMs
    };

    xQueueOverwrite(led2CommandQueue, &command);
}

void Led2CommandWorkerTask(void* pvParameters) {
    (void)pvParameters;

    Led2Command command;

    while (true) {
        if (xQueueReceive(led2CommandQueue, &command, portMAX_DELAY) != pdTRUE) {
            continue;
        }

        bool hasPendingCommand = true;

        while (hasPendingCommand) {
            hasPendingCommand = false;

            Led2StateSnapshot snapshot = getLed2StateSnapshot();
            int currentBrightness = clampBrightness(snapshot.appliedBrightness);
            int targetBrightness = command.power ? clampBrightness(command.brightness) : 0;

            if (command.transitionMs <= 0 || currentBrightness == targetBrightness) {
                writeLed2Brightness(targetBrightness);
                setLed2AppliedBrightness(targetBrightness);
            } else {
                int steps = command.transitionMs / TRANSITION_STEP_MS;
                if (steps < 1) {
                    steps = 1;
                }

                for (int i = 1; i <= steps; ++i) {
                    int value = currentBrightness + ((targetBrightness - currentBrightness) * i) / steps;
                    writeLed2Brightness(value);
                    setLed2AppliedBrightness(value);

                    Led2Command pendingCommand;
                    if (xQueueReceive(
                        led2CommandQueue,
                        &pendingCommand,
                        pdMS_TO_TICKS(TRANSITION_STEP_MS)
                    ) == pdTRUE) {
                        command = pendingCommand;
                        hasPendingCommand = true;
                        break;
                    }
                }

                if (hasPendingCommand) {
                    continue;
                }

                writeLed2Brightness(targetBrightness);
                setLed2AppliedBrightness(targetBrightness);
            }

            Led2Command pendingCommand;
            if (xQueueReceive(led2CommandQueue, &pendingCommand, 0) == pdTRUE) {
                command = pendingCommand;
                hasPendingCommand = true;
            }
        }
    }
}

CapabilityDefinition buildLight1PowerCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "switch.power";
    capability.endpointId = "led1";
    capability.supportedOperations = { "set" };

    capability.getStateJson = []() {
        return boolStateJson(led1Power);
        };

    capability.handleCommand = [](const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateJson, std::string& outError) {
            if (operation != "set") {
                outError = "operation_not_supported";
                return false;
            }

            led1Power = commandValue["value"].as<bool>();
            outStateJson = boolStateJson(led1Power);

            if (led1Power) {
                Serial.println("Light 1 turned ON");
                digitalWrite(LED1_PIN, HIGH);
            } else {
                Serial.println("Light 1 turned OFF");
                digitalWrite(LED1_PIN, LOW);
            }
            return true;
        };

    return capability;
}

CapabilityDefinition buildLight2PowerCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "switch.power";
    capability.supportedOperations = { "set" };
    capability.endpointId = "led2";

    capability.getStateJson = []() {
        return boolStateJson(getLed2StateSnapshot().power);
        };

    capability.handleCommand = [](const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateJson, std::string& outError) {
            if (operation != "set") {
                outError = "operation_not_supported";
                return false;
            }

            bool power = commandValue["value"].as<bool>();
            setLed2Power(power);
            outStateJson = boolStateJson(power);
            enqueueLed2Command(0);

            if (power) {
                Serial.println("Light 2 turned ON");
            } else {
                Serial.println("Light 2 turned OFF");
            }

            return true;
        };

    return capability;
}

CapabilityDefinition buildLight2BrightnessCapability() {
    CapabilityDefinition capability;
    capability.capabilityId = "light.brightness";
    capability.supportedOperations = { "set" };
    capability.endpointId = "led2";

    capability.getStateJson = []() {
        return "{\"value\":" + std::to_string(getLed2StateSnapshot().brightness) + "}";
        };

    capability.handleCommand = [](const std::string& operation, JsonVariantConst commandValue,
        std::string& outStateJson, std::string& outError) {
            if (operation != "set") {
                outError = "operation_not_supported";
                return false;
            }

            int brightness = commandValue["value"].as<int>();
            if (brightness < 0 || brightness > 100) {
                outError = "invalid_value";
                return true;
            }

            int transitionMs = commandValue["transitionMs"].as<int>() | 0;
            if (transitionMs < 0) {
                outError = "invalid_value";
                return true;
            }

            setLed2Brightness(brightness);
            outStateJson = "{\"value\":" + std::to_string(brightness) + "}";
            enqueueLed2Command(transitionMs);

            return true;
        };

    return capability;
}

std::vector<EndpointDefinition> endpoints = { endpoint1, endpoint2 };

std::vector<CapabilityDefinition> capabilities = {
    buildLight1PowerCapability(),
    buildLight2PowerCapability(),
    buildLight2BrightnessCapability()
};

Device device(apSsid, apPassword, deviceName, firmwareVersion, endpoints, capabilities);

void setup() {
    Serial.begin(115200);

    pinMode(RESET_PIN, INPUT_PULLUP);
    pinMode(LED1_PIN, OUTPUT);

    digitalWrite(LED1_PIN, LOW);
    ledcSetup(LED2_PWM_CHANNEL, LED2_PWM_FREQUENCY_HZ, LED2_PWM_RESOLUTION_BITS);
    ledcAttachPin(LED2_PIN, LED2_PWM_CHANNEL);
    writeLed2Brightness(0);

    led2StateMutex = xSemaphoreCreateMutex();
    led2CommandQueue = xQueueCreate(LED2_COMMAND_QUEUE_LENGTH, sizeof(Led2Command));

    if (led2StateMutex == nullptr || led2CommandQueue == nullptr) {
        Serial.println("Failed to initialize LED2 command worker resources");
    } else {
        BaseType_t created = xTaskCreate(
            Led2CommandWorkerTask,
            "Led2CommandWorker",
            3072,
            NULL,
            1,
            NULL
        );

        if (created == pdPASS) {
            enqueueLed2Command(0);
        } else {
            Serial.println("Failed to start LED2 command worker");
        }
    }

    device.begin();
}

unsigned long resetPressStart = 0;
bool wasPressed = false;

void loop() {
    if (digitalRead(RESET_PIN) == LOW) {
        if (!wasPressed) {
            resetPressStart = millis();
            wasPressed = true;
        } else if (millis() - resetPressStart >= RESET_HOLD_MS) {
            Serial.println("Resetting configuration...");
            device.resetConfiguration();
            ESP.restart();
        }
    } else {
        wasPressed = false;
    }
    device.loop();
}
