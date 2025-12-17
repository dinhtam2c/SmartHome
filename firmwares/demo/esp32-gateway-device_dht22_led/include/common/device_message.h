#pragma once

#include <string>

enum DeviceMessageType {
    DEVICE_PROVISION,
    DEVICE_DATA,
    DEVICE_STATES,
    DEVICE_ACTUATORS_STATES,

    DEVICE_PROVISION_RESPONSE,
    DEVICE_COMMAND
};

struct TransportMessage {
    std::string deviceId;
    DeviceMessageType messageType;
    const std::string& payload;
};
