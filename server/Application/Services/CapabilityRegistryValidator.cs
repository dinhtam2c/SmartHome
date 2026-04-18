using Application.Commands.Devices.ProvisionDevice;
using Application.Exceptions;

namespace Application.Services;

public interface ICapabilityRegistryValidator
{
    IEnumerable<DeviceEndpointModel> ValidateAndNormalize(IEnumerable<DeviceEndpointModel> endpoints);
}

public class CapabilityRegistryValidator : ICapabilityRegistryValidator
{
    private readonly ICapabilityRegistry _capabilityRegistry;

    public CapabilityRegistryValidator(ICapabilityRegistry capabilityRegistry)
    {
        _capabilityRegistry = capabilityRegistry;
    }

    public IEnumerable<DeviceEndpointModel> ValidateAndNormalize(IEnumerable<DeviceEndpointModel> endpoints)
    {
        var input = endpoints?.ToList() ?? [];
        if (input.Count == 0)
            return input;

        var duplicateEndpointIds = input
            .GroupBy(endpoint => endpoint.EndpointId?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateEndpointIds.Count > 0)
        {
            throw new InvalidCapabilityProvisionException(
                $"Provisioning endpoint list contains duplicate endpointId values: {string.Join(", ", duplicateEndpointIds)}");
        }

        var normalizedEndpoints = new List<DeviceEndpointModel>(input.Count);

        foreach (var endpoint in input)
        {
            if (string.IsNullOrWhiteSpace(endpoint.EndpointId))
            {
                throw new InvalidCapabilityProvisionException(
                    "EndpointId must not be empty");
            }

            var endpointId = endpoint.EndpointId.Trim();
            var endpointCapabilities = endpoint.Capabilities?.ToList() ?? [];
            if (endpointCapabilities.Count == 0)
            {
                throw new InvalidCapabilityProvisionException(
                    $"Endpoint '{endpointId}' must contain at least one capability");
            }

            var duplicateCapabilityIds = endpointCapabilities
                .GroupBy(capability => capability.CapabilityId?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateCapabilityIds.Count > 0)
            {
                throw new InvalidCapabilityProvisionException(
                    $"Endpoint '{endpointId}' contains duplicate capability ids: {string.Join(", ", duplicateCapabilityIds)}");
            }

            var normalizedCapabilities = new List<DeviceCapabilityModel>(endpointCapabilities.Count);
            foreach (var capability in endpointCapabilities)
            {
                ValidateCapabilityShape(capability);

                if (!_capabilityRegistry.TryGetDefinition(
                        capability.CapabilityId,
                        capability.CapabilityVersion,
                        out var definition))
                {
                    throw new InvalidCapabilityProvisionException(
                        $"Capability '{capability.CapabilityId}@{capability.CapabilityVersion}' is not found in capability registry");
                }

                var normalizedSupportedOperations = NormalizeOperations(capability.SupportedOperations).ToList();
                if (normalizedSupportedOperations.Count == 0)
                {
                    normalizedSupportedOperations = definition.Operations.Keys
                        .Select(operation => operation.Trim().ToLowerInvariant())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var unsupportedOperations = normalizedSupportedOperations
                    .Where(operation => !definition.SupportsOperation(operation))
                    .ToList();

                if (unsupportedOperations.Count > 0)
                {
                    throw new InvalidCapabilityProvisionException(
                        $"Capability '{capability.CapabilityId}@{capability.CapabilityVersion}' contains unsupported operations: {string.Join(", ", unsupportedOperations)}");
                }

                normalizedCapabilities.Add(capability with
                {
                    CapabilityId = capability.CapabilityId.Trim(),
                    SupportedOperations = normalizedSupportedOperations
                });
            }

            normalizedEndpoints.Add(endpoint with
            {
                EndpointId = endpointId,
                Name = string.IsNullOrWhiteSpace(endpoint.Name) ? null : endpoint.Name.Trim(),
                Capabilities = normalizedCapabilities
            });
        }

        return normalizedEndpoints;
    }

    private static void ValidateCapabilityShape(DeviceCapabilityModel capability)
    {
        if (string.IsNullOrWhiteSpace(capability.CapabilityId))
        {
            throw new InvalidCapabilityProvisionException("CapabilityId must not be empty");
        }

        if (capability.CapabilityVersion <= 0)
        {
            throw new InvalidCapabilityProvisionException(
                $"Capability '{capability.CapabilityId}' must include a positive version");
        }
    }

    private static IEnumerable<string> NormalizeOperations(IEnumerable<string>? operations)
    {
        if (operations is null)
            return [];

        return operations
            .Where(op => !string.IsNullOrWhiteSpace(op))
            .Select(op => op.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

}
