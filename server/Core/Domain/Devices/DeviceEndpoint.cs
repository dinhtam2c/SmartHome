namespace Core.Domain.Devices;

public class DeviceEndpoint
{
    private readonly List<DeviceCapability> _capabilities = [];

    public Guid Id { get; }
    public Guid DeviceId { get; }
    public string EndpointId { get; private set; }
    public string? Name { get; private set; }
    public IReadOnlyCollection<DeviceCapability> Capabilities => _capabilities;

    private DeviceEndpoint()
    {
        Id = Guid.Empty;
        DeviceId = Guid.Empty;
        EndpointId = string.Empty;
    }

    public DeviceEndpoint(Guid deviceId, string endpointId, string? name = null)
    {
        if (deviceId == Guid.Empty)
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        if (string.IsNullOrWhiteSpace(endpointId))
            throw new ArgumentException("EndpointId is required.", nameof(endpointId));

        Id = Guid.NewGuid();
        DeviceId = deviceId;
        EndpointId = endpointId.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    public void ReplaceCapabilities(IEnumerable<DeviceCapability> capabilities)
    {
        _capabilities.Clear();

        foreach (var capability in capabilities)
        {
            if (capability.EndpointId != Id)
            {
                throw new InvalidOperationException(
                    $"Capability '{capability.CapabilityId}' does not belong to endpoint '{EndpointId}'.");
            }

            _capabilities.Add(capability);
        }
    }

    public DeviceCapability? FindCapability(string capabilityId)
    {
        return _capabilities.FirstOrDefault(capability =>
            capability.CapabilityId.Equals(capabilityId, StringComparison.OrdinalIgnoreCase));
    }
}
