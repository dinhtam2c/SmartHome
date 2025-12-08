#include "gateway/utils.h"

#include <Arduino.h>
#include <time.h>

#include <sstream>

#include "gateway/config.h"

void syncTimeAndWait() {
    configTime(0, 0, NTP_SERVER_1, NTP_SERVER_2, NTP_SERVER_3);
    Serial.println("Waiting for time sync");
    while (time(nullptr) < 100000) {
        Serial.print('.');
        delay(500);
    }
    Serial.println("Time synchronized!");
}

std::vector<std::string> split(const std::string& s, char delimiter) {
    std::vector<std::string> tokens;
    std::string token;
    std::istringstream tokenStream(s);
    while (std::getline(tokenStream, token, delimiter)) {
        tokens.push_back(token);
    }
    return tokens;
}

bool strcmpsuf(const char* s, const char* suffix) {
    int l2 = strlen(suffix);
    if (l2 == 0)
        return false;

    int l1 = strlen(s);

    if (l1 < l2)
        return false;

    return strcmp(s + l1 - l2, suffix) == 0;
}
