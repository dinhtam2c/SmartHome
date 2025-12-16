#include <Arduino.h>

#include "gateway/Gateway.h"
#include "gateway/config.h"
#include "device/Device.h"
#include "device/GatewayTransport.h"
#include "device/config.h"

DirectGatewayTransport gatewayTransport;
Device device(&gatewayTransport);
Gateway gateway;

void setup() {
    Serial.begin(115200);
    Serial.println("..............................................");

    device.loadPreferences();
    gateway.begin();
    device.begin();
}

void loop() {
    gateway.loop();
    device.loop();

    delay(100);
}
