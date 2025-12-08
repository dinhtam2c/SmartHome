#pragma once

#include "common/device_message.h"

#include <Arduino.h>

class IDeviceTransport;

enum ProtocolType {
    PROTOCOL_DIRECT,
    PROTOCOL_WIFI,
    PROTOCOL_BLE,
    PROTOCOL_ZIGBEE
};

struct DeviceInfo {
    std::string deviceId;
    std::string identifier;
    std::string localId;
    ProtocolType protocol;
    IDeviceTransport* transport;
};

class IDeviceTransport {
public:
    virtual ~IDeviceTransport() = default;

    virtual void initialize() = 0;
    virtual void shutdown() = 0;

    virtual void startScan() = 0;

    virtual bool connectDevice(const std::string& identifier) = 0;

    virtual bool sendProvisionResponse(const std::string& identifier, const std::string& payload) = 0;

    virtual bool sendCommand(const std::string& identifier, const std::string& payload) = 0;

    virtual void registerCallbacks(
        std::function<void(const TransportMessage&)> onMessage,
        std::function<void(IDeviceTransport* transporter, const DeviceInfo&)> onDeviceDiscovered,
        std::function<void(const std::string&)> onDeviceDisconnected
    ) = 0;
};
