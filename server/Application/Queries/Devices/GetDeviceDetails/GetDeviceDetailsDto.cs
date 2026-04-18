namespace Application.Queries.Devices.GetDeviceDetails;

public sealed record DeviceDetailsDto(
    Guid Id,
    string Name,
    string FirmwareVersion,
    long? ProvisionedAt,

    bool IsOnline,
    long LastSeenAt,
    long Uptime,

    Guid? HomeId,
    string? HomeName,
    Guid? RoomId,
    string? RoomName,

    IEnumerable<DeviceEndpointDetailsDto> Endpoints
);

public sealed record DeviceEndpointDetailsDto(
    Guid Id,
    string EndpointId,
    string? Name,
    IEnumerable<DeviceCapabilityDetailsDto> Capabilities
);

public sealed record DeviceCapabilityDetailsDto(
    string CapabilityId,
    int CapabilityVersion,
    IEnumerable<string>? SupportedOperations,
    long LastReportedAt,
    IReadOnlyDictionary<string, object?>? State
);
