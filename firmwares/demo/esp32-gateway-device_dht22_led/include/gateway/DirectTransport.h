#pragma once

#include <vector>

#include "gateway/DeviceTransport.h"

class DirectTransport : public IDeviceTransport {
public:
    void initialize() override;

    void shutdown() override;

    void startScan() override;

    bool connectDevice(const std::string& identifier) override;

    bool sendProvisionResponse(const std::string& identifier, const std::string& payload) override;

    bool sendCommand(const std::string& identifier, const std::string& payload) override;

    void registerCallbacks(
        std::function<void(const TransportMessage&)> onMessage,
        std::function<void(IDeviceTransport* transporter, const DeviceInfo&)> onDeviceDiscovered,
        std::function<void(const std::string&)> onDeviceDisconnected
    ) override;

    void handleDeviceMessage(const TransportMessage& message);

private:
    std::vector<DeviceInfo> _connectedDevices;

    std::function<void(const TransportMessage&)> _onMessage;
    std::function<void(IDeviceTransport* transporter, const DeviceInfo&)> _onDeviceDiscovered;
    std::function<void(const std::string&)> _onDeviceDisconnected;
};
