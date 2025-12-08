#include "device/GatewayTransport.h"
#include "gateway/DirectTransport.h"

extern DirectTransport directTransport;

void DirectGatewayTransport::send(const TransportMessage& message) {
    directTransport.handleDeviceMessage(message);
}
