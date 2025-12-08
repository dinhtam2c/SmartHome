#pragma once

#include <vector>

#include "gateway/DeviceTransport.h"

class Gateway;

class DeviceManager {
public:
    void begin(Gateway* gateway);

    void loop();

    bool routeProvisionResponse(const std::string& deviceId, const std::string& payload);

    bool routeCommand(const std::string& deviceId, const std::string& payload);

private:
    Gateway* _gateway;
    std::vector<DeviceInfo> _connectedDevices;
    std::vector<IDeviceTransport*> _tranporters;
    unsigned long _lastScanTime = 0;

    void registerAdapter(IDeviceTransport* transporter);

    void handleDeviceMessage(const TransportMessage& message);

    void handleDiscovery(IDeviceTransport* transporter, const DeviceInfo& info);

    void handleDisconnect(const std::string&);
};
