#include "ProvisioningPortalRoutes.h"

void configureProvisioningPortalRoutes(
    WebServer& server,
    std::function<std::string()> renderPortalPage,
    std::function<std::string()> renderProvisionStatus,
    std::function<std::string()> renderWifiNetworksJson,
    std::function<void()> handleWifiScan,
    std::function<void()> handleSave,
    std::function<void()> handleNotFound) {
    auto sendPortalPage = [&server, renderPortalPage](int statusCode = 200) {
        std::string page = renderPortalPage();
        server.sendHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0", true);
        server.sendHeader("Pragma", "no-cache", true);
        server.sendHeader("Expires", "0", true);
        server.send(statusCode, "text/html", String(page.c_str()));
        };

    server.on("/", HTTP_ANY, [sendPortalPage]() {
        sendPortalPage(200);
        });

    server.on("/generate_204", HTTP_ANY, [sendPortalPage]() {
        sendPortalPage(200);
        });

    server.on("/gen_204", HTTP_ANY, [sendPortalPage]() {
        sendPortalPage(200);
        });

    server.on("/hotspot-detect.html", HTTP_ANY, [sendPortalPage]() {
        sendPortalPage(200);
        });

    server.on("/connecttest.txt", HTTP_ANY, [sendPortalPage]() {
        sendPortalPage(200);
        });

    server.on("/ncsi.txt", HTTP_ANY, [sendPortalPage]() {
        sendPortalPage(200);
        });

    server.on("/status", HTTP_ANY, [&server, renderProvisionStatus]() {
        std::string status = renderProvisionStatus();
        server.sendHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0", true);
        server.sendHeader("Pragma", "no-cache", true);
        server.sendHeader("Expires", "0", true);
        server.send(200, "text/html", String(status.c_str()));
        });

    server.on("/wifi-networks", HTTP_ANY, [&server, renderWifiNetworksJson]() {
        if (server.method() != HTTP_GET) {
            server.send(405, "application/json", "{\"error\":\"method_not_allowed\"}");
            return;
        }

        std::string payload = renderWifiNetworksJson();
        server.sendHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0", true);
        server.sendHeader("Pragma", "no-cache", true);
        server.sendHeader("Expires", "0", true);
        server.send(200, "application/json", String(payload.c_str()));
        });

    server.on("/wifi-scan", HTTP_ANY, [&server, handleWifiScan]() {
        if (server.method() != HTTP_POST) {
            server.send(405, "application/json", "{\"error\":\"method_not_allowed\"}");
            return;
        }

        handleWifiScan();
        });

    server.on("/save", HTTP_ANY, [sendPortalPage, handleSave, &server]() {
        if (server.method() != HTTP_POST) {
            sendPortalPage(200);
            return;
        }
        handleSave();
        });

    server.onNotFound([handleNotFound]() {
        handleNotFound();
        });
}
