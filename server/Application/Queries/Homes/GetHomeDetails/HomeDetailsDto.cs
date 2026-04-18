namespace Application.Queries.Homes.GetHomeDetails;

public sealed record HomeDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    long CreatedAt,
    int RoomCount,
    int DeviceCount,
    int OnlineDeviceCount,
    IEnumerable<RoomOverviewDto> Rooms,
    IEnumerable<HomeUnassignedDeviceOverviewDto> UnassignedDevices,
    IEnumerable<HomeSceneSummaryDto> Scenes
);

public sealed record RoomOverviewDto(
    Guid Id,
    string Name,
    string? Description,
    int DeviceCount,
    int OnlineDeviceCount,
    double? Temperature,
    double? Humidity
);

public sealed record HomeSceneSummaryDto(
    Guid Id,
    string Name,
    bool IsEnabled
);

public sealed record HomeUnassignedDeviceOverviewDto(
    Guid Id,
    string Name,
    bool IsOnline,
    IEnumerable<HomeUnassignedDeviceEndpointOverviewDto> Endpoints
);

public sealed record HomeUnassignedDeviceEndpointOverviewDto(
    string EndpointId,
    string? Name,
    IEnumerable<HomeUnassignedCapabilityOverviewDto> Capabilities
);

public sealed record HomeUnassignedCapabilityOverviewDto(
    string CapabilityId,
    int CapabilityVersion,
    IEnumerable<string>? SupportedOperations,
    long LastReportedAt,
    IReadOnlyDictionary<string, object?>? State
);
