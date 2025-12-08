#include "gateway/DeviceManager.h"

#include "gateway/Gateway.h"
#include "gateway/DirectTransport.h"

DirectTransport directTransport;

std::string whitelist[] = {
    "_1"
};

void DeviceManager::begin(Gateway* gateway) {
    _gateway = gateway;
    directTransport.initialize();
    registerAdapter(&directTransport);
    directTransport.startScan();
}

void DeviceManager::loop() {}

void DeviceManager::registerAdapter(IDeviceTransport* transporter) {
    _tranporters.push_back(transporter);
    transporter->registerCallbacks(
        [this](const TransportMessage& message) { handleDeviceMessage(message); },
        [this](IDeviceTransport* transporter, const DeviceInfo& deviceInfo) {
            handleDiscovery(transporter, deviceInfo);
        },
        [this](const std::string& identifier) { handleDisconnect(identifier); }
    );
}

bool DeviceManager::routeProvisionResponse(const std::string& deviceId, const std::string& payload) {
    for (auto& it : _connectedDevices) {
        if (it.identifier == deviceId || it.deviceId == deviceId) {
            return it.transport->sendProvisionResponse(it.identifier, payload);
        }
    }
    return false;
}

bool DeviceManager::routeCommand(const std::string& deviceId, const std::string& payload) {
    for (auto& it : _connectedDevices) {
        if (it.deviceId == deviceId) {
            return it.transport->sendCommand(it.identifier, payload);
        }
    }
    return false;
}

void DeviceManager::handleDeviceMessage(const TransportMessage& message) {
    _gateway->handleDeviceMessage(message);
}

void DeviceManager::handleDiscovery(IDeviceTransport* transporter, const DeviceInfo& info) {
    /* Check whitelist */
    for (const auto& id : whitelist) {
        if (info.identifier != id) {
            Serial.printf("Device %s not in whitelist, ignoring\r\n", info.identifier.c_str());
            return;
        }
    }

    /* Check connected */
    for (auto& device : _connectedDevices) {
        if (device.identifier == info.identifier) {
            return;
        }
    }

    /* Connect */
    Serial.printf("Connecting to device: %s\r\n", info.identifier.c_str());
    if (transporter->connectDevice(info.identifier)) {
        Serial.printf("Device connected: %s\r\n", info.identifier.c_str());
        _connectedDevices.push_back(info);
    }
}

void DeviceManager::handleDisconnect(const std::string&) {
    // TODO: notify server
}
