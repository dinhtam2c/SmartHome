namespace Application.Queries.Devices.GetDeviceCapabilityStateHistory;

public sealed record DeviceCapabilityStateHistoryDto(
    Guid Id,
    Guid DeviceId,
    string CapabilityId,
    string EndpointId,
    string StatePayload,
    long ReportedAt
);
