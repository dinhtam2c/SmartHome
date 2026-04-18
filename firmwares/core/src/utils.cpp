#include "utils.h"

#include <cctype>

std::string normalizeMacUpperWithColons(const std::string& macAddress) {
    std::string normalized = macAddress;
    for (char& ch : normalized) {
        if (ch == '-') {
            ch = ':';
            continue;
        }
        ch = static_cast<char>(std::toupper(static_cast<unsigned char>(ch)));
    }
    return normalized;
}