#pragma once

#include <string>

#include "Configuration.h"

enum class ProvisioningPortalState {
    Idle,
    ConfigRequired,
    WaitingForWifi,
    RequestingCode,
    WaitingForApproval,
    Provisioned,
    Error,
};

struct ProvisioningPortalViewModel {
    WifiPreference wifi;
    ServerPreference server;
    ProvisioningPortalState state = ProvisioningPortalState::Idle;
    std::string message;
    std::string provisionCode;
};

std::string renderProvisioningStatusHtml(const ProvisioningPortalViewModel& model);
std::string renderProvisioningPortalPage(const ProvisioningPortalViewModel& model);
