#pragma once

#include <WebServer.h>

#include <functional>
#include <string>

void configureProvisioningPortalRoutes(
    WebServer& server,
    std::function<std::string()> renderPortalPage,
    std::function<std::string()> renderProvisionStatus,
    std::function<std::string()> renderWifiNetworksJson,
    std::function<void()> handleWifiScan,
    std::function<void()> handleSave,
    std::function<void()> handleNotFound);
