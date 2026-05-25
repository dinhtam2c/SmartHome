using Domain.Common;

namespace Domain.Models.Devices;

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
        LastReportedAt = UnixTime.Now();
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
                || !StructuredValue.AreEqual(currentValue, item.Value))
            {
                changed = true;
            }

            _state[item.Key] = StructuredValue.Snapshot(item.Value);
        }

        LastReportedAt = UnixTime.Now();
        return changed;
    }
}
