#include "ProvisioningCommandSchemaValidator.h"

#include <cctype>
#include <unordered_set>

namespace {
    std::string normalizeOperationName(const std::string& operation) {
        std::string normalized;
        normalized.reserve(operation.size());
        for (char ch : operation) {
            normalized.push_back(static_cast<char>(std::tolower(static_cast<unsigned char>(ch))));
        }
        return normalized;
    }
}

bool validateProvisioningCommandSchemaForOperations(const CapabilityDefinition& capability, std::string& error) {
    std::unordered_set<std::string> seenOperations;

    for (const auto& operation : capability.supportedOperations) {
        std::string normalizedOperation = normalizeOperationName(operation);
        if (normalizedOperation.empty()) {
            error = "supportedOperations contains an empty operation name";
            return false;
        }

        auto inserted = seenOperations.insert(normalizedOperation);
        if (!inserted.second) {
            error = "supportedOperations contains duplicate operation names (case-insensitive)";
            return false;
        }
    }

    return true;
}
