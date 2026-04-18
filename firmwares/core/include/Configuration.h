#pragma once

#include <Preferences.h>
#include <cstdint>
#include <string>

struct WifiPreference {
    std::string ssid;
    std::string password;
};

struct ServerPreference {
    std::string address;
    uint16_t port;
};

struct CredentialPreference {
    std::string deviceId;
    std::string token;
};

class Configuration {
public:
    WifiPreference wifi() const;
    ServerPreference server() const;
    CredentialPreference credentials() const;

    void setWifi(const std::string& ssid, const std::string& password);
    void setServer(const std::string& address, uint16_t port);
    void setCredentials(const std::string& deviceId, const std::string& token);

    void commitDefaults();

    void reset();

private:
    static constexpr const char* WIFI_NAMESPACE = "wifi";
    static constexpr const char* SERVER_NAMESPACE = "server";
    static constexpr const char* CREDENTIALS_NAMESPACE = "credentials";

    static constexpr const char* WIFI_SSID_KEY = "ssid";
    static constexpr const char* WIFI_PASSWORD_KEY = "password";
    static constexpr const char* SERVER_ADDRESS_KEY = "address";
    static constexpr const char* SERVER_PORT_KEY = "port";
    static constexpr const char* DEVICE_ID_KEY = "deviceId";
    static constexpr const char* TOKEN_KEY = "token";

    static constexpr const char* DEFAULT_WIFI_SSID = "";
    static constexpr const char* DEFAULT_WIFI_PASSWORD = "";
    static constexpr const char* DEFAULT_SERVER_ADDRESS = "";
    static constexpr uint16_t DEFAULT_SERVER_PORT = 1883;
    static constexpr const char* DEFAULT_DEVICE_ID = "";
    static constexpr const char* DEFAULT_TOKEN = "";
};
