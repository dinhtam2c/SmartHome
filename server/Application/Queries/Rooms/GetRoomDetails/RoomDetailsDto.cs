namespace Application.Queries.Rooms.GetRoomDetails;

public sealed record RoomDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    long CreatedAt,
    int DeviceCount,
    int OnlineDeviceCount,
    double? Temperature,
    double? Humidity,
    IEnumerable<DeviceOverviewDto> Devices
);

public sealed record DeviceOverviewDto(
    Guid Id,
    string Name,
    bool IsOnline,
    IEnumerable<DeviceEndpointOverviewDto> Endpoints
);

public sealed record DeviceEndpointOverviewDto(
    string EndpointId,
    string? Name,
    IEnumerable<CapabilityOverviewDto> Capabilities
);

public sealed record CapabilityOverviewDto(
    string CapabilityId,
    int CapabilityVersion,
    IEnumerable<string>? SupportedOperations,
    long LastReportedAt,
    IReadOnlyDictionary<string, object?>? State
);
