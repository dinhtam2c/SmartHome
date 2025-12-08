#pragma once
#include "common/device_message.h"

class IGatewayTransport {
public:
    virtual void send(const TransportMessage& message) = 0;
};

class DirectGatewayTransport : public IGatewayTransport {
public:
    void send(const TransportMessage& message) override;
};
