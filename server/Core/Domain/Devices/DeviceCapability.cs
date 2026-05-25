using System.Text.Json;
using Core.Common;

namespace Core.Domain.Devices;

public class DeviceCapability
{
    public Guid Id { get; }
    public Guid EndpointId { get; private set; }
    public string CapabilityId { get; private set; }
    public int CapabilityVersion { get; private set; }

    public IEnumerable<string>? SupportedOperations { get; private set; }

    private readonly Dictionary<string, object?> _state = [];
    public IReadOnlyDictionary<string, object?> State => _state;

    public Dictionary<string, object>? RuntimeHints { get; set; }

    public long LastReportedAt { get; private set; }

    private DeviceCapability()
    {
        EndpointId = Guid.Empty;
        CapabilityId = string.Empty;
        CapabilityVersion = 1;
        SupportedOperations = [];
    }

    public DeviceCapability(Guid endpointId, string capabilityId, int capabilityVersion,
        IEnumerable<string>? supportedOperations = null)
    {
        if (endpointId == Guid.Empty)
            throw new ArgumentException("EndpointId is required.", nameof(endpointId));

        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("CapabilityId is required.", nameof(capabilityId));

        if (capabilityVersion <= 0)
            throw new ArgumentException("CapabilityVersion must be greater than 0.", nameof(capabilityVersion));

        Id = Guid.NewGuid();
        EndpointId = endpointId;
        CapabilityId = capabilityId;
        CapabilityVersion = capabilityVersion;
        SupportedOperations = supportedOperations;
        LastReportedAt = Time.UnixNow();
    }

    public bool Matches(string capabilityId)
    {
        return CapabilityId.Equals(capabilityId, StringComparison.OrdinalIgnoreCase);
    }

    public bool SupportsOperation(string operation)
    {
        if (SupportedOperations is null)
            return false;

        return SupportedOperations.Contains(operation, StringComparer.OrdinalIgnoreCase);
    }

    public bool UpdateState(Dictionary<string, object?> state)
    {
        var changed = false;
        foreach (var item in state)
        {
            if (!_state.TryGetValue(item.Key, out var currentValue)
                || !JsonValuesEqual(currentValue, item.Value))
            {
                changed = true;
            }

            _state[item.Key] = item.Value;
        }

        LastReportedAt = Time.UnixNow();
        return changed;
    }

    private static bool JsonValuesEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return JsonSerializer.Serialize(left) == JsonSerializer.Serialize(right);
    }
}
