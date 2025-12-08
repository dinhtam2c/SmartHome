#pragma once

#include <string>

enum DeviceMessageType {
    DEVICE_PROVISION,
    DEVICE_DATA,
    DEVICE_STATE,

    DEVICE_PROVISION_RESPONSE,
    DEVICE_COMMAND
};

struct TransportMessage {
    std::string deviceId;
    DeviceMessageType messageType;
    std::string payload;
};
