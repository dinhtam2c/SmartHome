namespace Application.Queries.Homes.GetHomeDevices;

public sealed record HomeDeviceDto(
    Guid Id,
    string Name,
    string FirmwareVersion,
    bool IsOnline,
    long ProvisionedAt,
    long LastSeenAt,
    long Uptime,
    Guid? RoomId,
    string? RoomName,
    IReadOnlyList<HomeDeviceEndpointDto> Endpoints
);

public sealed record HomeDeviceEndpointDto(
    string EndpointId,
    string? Name,
    IReadOnlyList<HomeDeviceCapabilityDto> Capabilities
);

public sealed record HomeDeviceCapabilityDto(
    string CapabilityId,
    int CapabilityVersion,
    IEnumerable<string>? SupportedOperations,
    long LastReportedAt,
    IReadOnlyDictionary<string, object?> State
);
