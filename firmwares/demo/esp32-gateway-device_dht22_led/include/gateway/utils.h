#pragma once

#include <vector>
#include <string>

void syncTimeAndWait();
std::vector<std::string> split(const std::string& s, char delimiter);
bool strcmpsuf(const char* s, const char* suffix);
