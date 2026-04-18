#include "Configuration.h"

WifiPreference Configuration::wifi() const {
    WifiPreference preference;
    Preferences preferences;
    if (preferences.begin(WIFI_NAMESPACE, true)) {
        preference.ssid = preferences.getString(WIFI_SSID_KEY, DEFAULT_WIFI_SSID).c_str();
        preference.password = preferences.getString(WIFI_PASSWORD_KEY, DEFAULT_WIFI_PASSWORD).c_str();
        preferences.end();
    }
    return preference;
}

ServerPreference Configuration::server() const {
    ServerPreference preference;
    Preferences preferences;
    if (preferences.begin(SERVER_NAMESPACE, true)) {
        preference.address = preferences.getString(SERVER_ADDRESS_KEY, DEFAULT_SERVER_ADDRESS).c_str();
        preference.port = preferences.getUShort(SERVER_PORT_KEY, DEFAULT_SERVER_PORT);
        preferences.end();
    }
    return preference;
}

CredentialPreference Configuration::credentials() const {
    CredentialPreference preference;
    Preferences preferences;
    if (preferences.begin(CREDENTIALS_NAMESPACE, true)) {
        preference.deviceId = preferences.getString(DEVICE_ID_KEY, DEFAULT_DEVICE_ID).c_str();
        preference.token = preferences.getString(TOKEN_KEY, DEFAULT_TOKEN).c_str();
        preferences.end();
    }
    return preference;
}

void Configuration::setWifi(const std::string& ssid, const std::string& password) {
    Preferences preferences;
    if (preferences.begin(WIFI_NAMESPACE, false)) {
        preferences.putString(WIFI_SSID_KEY, ssid.c_str());
        preferences.putString(WIFI_PASSWORD_KEY, password.c_str());
        preferences.end();
    }
}

void Configuration::setServer(const std::string& address, uint16_t port) {
    Preferences preferences;
    if (preferences.begin(SERVER_NAMESPACE, false)) {
        preferences.putString(SERVER_ADDRESS_KEY, address.c_str());
        preferences.putUShort(SERVER_PORT_KEY, port);
        preferences.end();
    }
}

void Configuration::setCredentials(const std::string& deviceId, const std::string& token) {
    Preferences preferences;
    if (preferences.begin(CREDENTIALS_NAMESPACE, false)) {
        preferences.putString(DEVICE_ID_KEY, deviceId.c_str());
        preferences.putString(TOKEN_KEY, token.c_str());
        preferences.end();
    }
}

void Configuration::commitDefaults() {
    Preferences wifiPreferences;
    if (wifiPreferences.begin(WIFI_NAMESPACE, false)) {
        if (!wifiPreferences.isKey(WIFI_SSID_KEY)) {
            wifiPreferences.putString(WIFI_SSID_KEY, DEFAULT_WIFI_SSID);
            wifiPreferences.putString(WIFI_PASSWORD_KEY, DEFAULT_WIFI_PASSWORD);
        }
        wifiPreferences.end();
    }

    Preferences serverPreferences;
    if (serverPreferences.begin(SERVER_NAMESPACE, false)) {
        if (!serverPreferences.isKey(SERVER_ADDRESS_KEY)) {
            serverPreferences.putString(SERVER_ADDRESS_KEY, DEFAULT_SERVER_ADDRESS);
            serverPreferences.putUShort(SERVER_PORT_KEY, DEFAULT_SERVER_PORT);
        }
        serverPreferences.end();
    }

    Preferences credentialPreferences;
    if (credentialPreferences.begin(CREDENTIALS_NAMESPACE, false)) {
        if (!credentialPreferences.isKey(DEVICE_ID_KEY)) {
            credentialPreferences.putString(DEVICE_ID_KEY, DEFAULT_DEVICE_ID);
            credentialPreferences.putString(TOKEN_KEY, DEFAULT_TOKEN);
        }
        credentialPreferences.end();
    }
}

void Configuration::reset() {
    Preferences wifiPreferences;
    if (wifiPreferences.begin(WIFI_NAMESPACE, false)) {
        wifiPreferences.clear();
        wifiPreferences.end();
    }

    Preferences serverPreferences;
    if (serverPreferences.begin(SERVER_NAMESPACE, false)) {
        serverPreferences.clear();
        serverPreferences.end();
    }

    Preferences credentialPreferences;
    if (credentialPreferences.begin(CREDENTIALS_NAMESPACE, false)) {
        credentialPreferences.clear();
        credentialPreferences.end();
    }
}
