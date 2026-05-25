namespace Application.UseCases.Floors.GetFloor;

public sealed record FloorDto(
    Guid Id,
    Guid HomeId,
    string Name,
    int SortOrder,
    int CanvasWidth,
    int CanvasHeight,
    long CreatedAt,
    long UpdatedAt,
    IReadOnlyList<FloorPlanRoomDto> FloorPlanRooms,
    IReadOnlyList<FloorDevicePlacementDto> DevicePlacements
);

public sealed record FloorPlanRoomDto(
    Guid Id,
    Guid RoomId,
    IReadOnlyList<FloorPointDto> Polygon,
    string? FillColor
);

public sealed record FloorDevicePlacementDto(
    Guid Id,
    Guid DeviceId,
    float X,
    float Y
);

public sealed record FloorPointDto(float X, float Y);
