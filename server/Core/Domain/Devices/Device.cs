using System.Security.Cryptography;
using Core.Common;
using Core.Primitives;

namespace Core.Domain.Devices;

public class Device : Entity
{
    public Guid Id { get; }
    public Guid? HomeId { get; private set; }
    public Guid? RoomId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string MacAddress { get; }   // TODO: change to value object
    public string FirmwareVersion { get; private set; } = string.Empty;
    public DeviceProtocol Protocol { get; private set; } = DeviceProtocol.DirectMqtt;

    public ProvisionState ProvisionState { get; private set; } = ProvisionState.PENDING;
    public string? ProvisionCode { get; private set; }
    public string AccessToken { get; private set; } = string.Empty;

    public long CreatedAt { get; private set; }
    public long? ProvisionedAt { get; private set; }

    // TODO: move runtime status to somewhere else
    public bool IsOnline { get; private set; }
    public long LastSeenAt { get; private set; }
    public long Uptime { get; private set; }

    private readonly List<DeviceEndpoint> _endpoints = [];
    public IReadOnlyCollection<DeviceEndpoint> Endpoints => _endpoints;

    // Transitional flattened view to keep application flows stable while queries are migrated.
    public IReadOnlyCollection<DeviceCapability> Capabilities =>
        _endpoints.SelectMany(endpoint => endpoint.Capabilities).ToList();

    private Device(string macAddress)
    {
        Id = Guid.NewGuid();
        MacAddress = macAddress;

        var now = Time.UnixNow();
        CreatedAt = now;
    }

    public static Device Create(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            throw new ArgumentException("Invalid Mac Address");

        return new(macAddress);
    }

    public void Reprovision(
        string name,
        string firmwareVersion,
        DeviceProtocol protocol,
        ICollection<DeviceEndpoint> endpoints,
        IReadOnlyCollection<CapabilityDefinition> capabilityDefinitions)
    {
        Name = name;
        FirmwareVersion = firmwareVersion;
        Protocol = protocol;

        ValidateProvisioningInvariants(endpoints, capabilityDefinitions);

        _endpoints.Clear();
        foreach (var endpoint in endpoints)
        {
            _endpoints.Add(endpoint);
        }

        ProvisionCode = GenerateProvisionCode();
        ProvisionState = ProvisionState.PENDING;
        ProvisionedAt = null;

        LastSeenAt = Time.UnixNow();

        Raise(new DeviceProvisionCodeGeneratedDomainEvent(Guid.NewGuid(), MacAddress, ProvisionCode));
    }

    private static void ValidateProvisioningInvariants(
        ICollection<DeviceEndpoint> endpoints,
        IReadOnlyCollection<CapabilityDefinition> capabilityDefinitions)
    {
        if (endpoints.Count == 0)
            throw new InvalidOperationException("Device must have at least one endpoint");

        var duplicateEndpointIds = endpoints
            .GroupBy(endpoint => endpoint.EndpointId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateEndpointIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate endpoint ids are not allowed: {string.Join(", ", duplicateEndpointIds)}");
        }

        var duplicateTargets = new List<string>();

        var definitionsByKey = capabilityDefinitions
            .ToDictionary(
                d => ToDefinitionKey(d.Id, d.Version),
                d => d,
                StringComparer.OrdinalIgnoreCase);

        foreach (var endpoint in endpoints)
        {
                if (string.IsNullOrWhiteSpace(endpoint.EndpointId))
            {
                throw new InvalidOperationException(
                    "EndpointId must not be empty.");
            }

            var duplicateCapabilities = endpoint.Capabilities
                .GroupBy(capability => capability.CapabilityId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            foreach (var duplicateCapability in duplicateCapabilities)
            {
                duplicateTargets.Add($"{duplicateCapability}@{endpoint.EndpointId}");
            }

            foreach (var capability in endpoint.Capabilities)
            {
                if (capability.EndpointId != endpoint.Id)
                {
                    throw new InvalidOperationException(
                        $"Capability '{capability.CapabilityId}@{capability.CapabilityVersion}' endpoint ownership is invalid.");
                }

                var key = ToDefinitionKey(capability.CapabilityId, capability.CapabilityVersion);
                if (!definitionsByKey.TryGetValue(key, out var definition))
                {
                    throw new InvalidOperationException(
                        $"Capability '{capability.CapabilityId}@{capability.CapabilityVersion}' does not exist in registry.");
                }

                var unsupportedOperations = (capability.SupportedOperations ?? [])
                    .Where(operation => !definition.SupportsOperation(operation))
                    .ToList();

                if (unsupportedOperations.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Capability '{capability.CapabilityId}@{capability.CapabilityVersion}' has unsupported operations: {string.Join(", ", unsupportedOperations)}");
                }
            }
        }

        if (duplicateTargets.Count > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate capabilities are not allowed per endpoint: {string.Join(", ", duplicateTargets)}");
        }
    }

    private static string ToDefinitionKey(string capabilityId, int capabilityVersion)
    {
        return $"{capabilityId.Trim().ToLowerInvariant()}@{capabilityVersion}";
    }

    public void ConfirmProvisioning(Guid homeId, Guid? roomId)
    {
        HomeId = homeId;
        RoomId = roomId;

        ProvisionState = ProvisionState.COMPLETED;
        ProvisionedAt = Time.UnixNow();
        ProvisionCode = null;
        AccessToken = GenerateAccessToken();

        Raise(new DeviceProvisionedDomainEvent(Guid.NewGuid(), MacAddress, Id, AccessToken));
    }

    public void AssignRoom(Guid roomId)
    {
        if (!HomeId.HasValue)
            throw new InvalidOperationException("Device must be assigned to a home before assigning a room");

        var previousRoomId = RoomId;
        RoomId = roomId;

        if (previousRoomId != roomId)
        {
            Raise(new DeviceRoomAssignedDomainEvent(
                Guid.NewGuid(),
                Id,
                HomeId.Value,
                previousRoomId,
                RoomId));
        }
    }

    public void MarkOnline()
    {
        bool isOnlineBefore = IsOnline == true;

        IsOnline = true;
        LastSeenAt = Time.UnixNow();

        if (!isOnlineBefore)
        {
            Raise(new DeviceWentOnlineDomainEvent(Guid.NewGuid(), Id, HomeId!.Value, RoomId));
        }
    }

    public void MarkOffline()
    {
        bool isOfflineBefore = IsOnline == false;

        IsOnline = false;
        Uptime = 0;

        if (!isOfflineBefore)
        {
            Raise(new DeviceWentOfflineDomainEvent(Guid.NewGuid(), Id, HomeId!.Value, RoomId));
        }
    }

    public void UpdateSystemState(int uptime)
    {
        Uptime = uptime;
        LastSeenAt = Time.UnixNow();

        if (HomeId.HasValue)
        {
            Raise(new DeviceSystemStateUpdatedDomainEvent(
                Guid.NewGuid(),
                Id,
                HomeId.Value,
                RoomId,
                Uptime,
                LastSeenAt));
        }
    }

    public void UpdateName(string name)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        Name = name;

        Raise(new DeviceInfoUpdatedDomainEvent(
            Guid.NewGuid(),
            Id,
            HomeId,
            RoomId,
            Name));
    }

    public void MarkDeleted()
    {
        Raise(new DeviceDeletedDomainEvent(
            Guid.NewGuid(),
            Id,
            HomeId,
            RoomId));
    }

    public DeviceCapability? FindCapability(string capabilityId)
    {
        return Capabilities.FirstOrDefault(c =>
            c.CapabilityId.Equals(capabilityId, StringComparison.OrdinalIgnoreCase));
    }

    public DeviceCapability? FindCapability(string capabilityId, string? endpointId)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
            return FindCapability(capabilityId);

        var endpoint = FindEndpoint(endpointId);
        return endpoint?.FindCapability(capabilityId);
    }

    public DeviceCapability? FindSingleCapability(string capabilityId, string? endpointId, out bool isAmbiguous)
    {
        var candidates = Capabilities
            .Where(c => c.CapabilityId.Equals(capabilityId, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(endpointId))
        {
            var endpoint = FindEndpoint(endpointId);
            isAmbiguous = false;
            return endpoint?.FindCapability(capabilityId);
        }

        var matched = candidates.ToList();
        isAmbiguous = string.IsNullOrWhiteSpace(endpointId) && matched.Count > 1;

        if (matched.Count == 0 || isAmbiguous)
            return null;

        return matched[0];
    }

    public bool UpdateCapabilityState(
        DeviceCapability capability,
        Dictionary<string, object?> normalizedState)
    {
        if (capability is null)
            return false;

        capability.UpdateState(normalizedState);
        var endpointId = ResolveEndpointId(capability.EndpointId);

        Raise(new DeviceCapabilityStateUpdatedDomainEvent(
            Id: Guid.NewGuid(),
            DeviceId: Id,
            CapabilityId: capability.CapabilityId,
            EndpointId: endpointId,
            ReportedAt: capability.LastReportedAt,
            State: new Dictionary<string, object?>(normalizedState, StringComparer.OrdinalIgnoreCase)));

        return true;
    }

    public DeviceEndpoint? FindEndpoint(string endpointId)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
            return null;

        return _endpoints.FirstOrDefault(endpoint =>
            endpoint.EndpointId.Equals(endpointId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private string ResolveEndpointId(Guid endpointId)
    {
        if (endpointId == Guid.Empty)
            throw new InvalidOperationException("EndpointId must not be empty.");

        return _endpoints
            .FirstOrDefault(endpoint => endpoint.Id == endpointId)
            ?.EndpointId
            ?? throw new InvalidOperationException(
                $"Endpoint '{endpointId}' was not found on device '{Id}'.");
    }

    private static string GenerateProvisionCode()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    private static string GenerateAccessToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

}

public enum ProvisionState
{
    PENDING,
    COMPLETED
}

public enum DeviceProtocol
{
    DirectMqtt,
    GatewayZigbee,
    MatterNative
}
