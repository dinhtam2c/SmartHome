namespace Core.Domain.Data;

public class DeviceCapabilityStateHistory
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public string CapabilityId { get; set; } = string.Empty;
    public string EndpointId { get; set; } = string.Empty;
    public string StatePayload { get; set; } = string.Empty;
    public long ReportedAt { get; set; }

    private DeviceCapabilityStateHistory()
    {
    }

    public DeviceCapabilityStateHistory(
        Guid deviceId,
        string capabilityId,
        string endpointId,
        long reportedAt,
        string statePayload)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        CapabilityId = capabilityId;
        EndpointId = endpointId;
        ReportedAt = reportedAt;
        StatePayload = statePayload;
    }
}
