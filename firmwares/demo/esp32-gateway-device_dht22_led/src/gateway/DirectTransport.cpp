#include "gateway/DirectTransport.h"

#include "device/Device.h"

extern Device device;
DeviceInfo internalDevice;

void DirectTransport::initialize() {
    internalDevice = {
        device.getDeviceId(),
        device.getIdentifier(),
        "",
        PROTOCOL_DIRECT,
        this
    };
}

void DirectTransport::shutdown() {
    _connectedDevices.clear();
}

void DirectTransport::startScan() {
    _onDeviceDiscovered(this, internalDevice);
}

bool DirectTransport::connectDevice(const std::string& identifier) {
    if (internalDevice.identifier == identifier) {
        _connectedDevices.push_back(internalDevice);
        return true;
    }

    return false;
}

void DirectTransport::handleDeviceMessage(const TransportMessage& message) {
    if (message.deviceId != internalDevice.deviceId && message.deviceId != internalDevice.identifier) {
        return;
    }

    if (_onMessage) {
        _onMessage(message);
    }
}

bool DirectTransport::sendProvisionResponse(const std::string& identifier, const std::string& payload) {
    device.handleGatewayMessage(DEVICE_PROVISION_RESPONSE, payload);
    return true;
}

bool DirectTransport::sendCommand(const std::string& identifier, const std::string& payload) {
    device.handleGatewayMessage(DEVICE_COMMAND, payload);
    return true;
}

void DirectTransport::registerCallbacks(
    std::function<void(const TransportMessage&)> onMessage,
    std::function<void(IDeviceTransport* transporter, const DeviceInfo&)> onDeviceDiscovered,
    std::function<void(const std::string&)> onDeviceDisconnected
) {
    _onMessage = onMessage;
    _onDeviceDiscovered = onDeviceDiscovered;
    _onDeviceDisconnected = onDeviceDisconnected;
}
