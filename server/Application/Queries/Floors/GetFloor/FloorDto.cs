namespace Application.Queries.Floors.GetFloor;

public sealed record FloorDto(
    Guid Id,
    Guid HomeId,
    string Name,
    int SortOrder,
    int CanvasWidth,
    int CanvasHeight,
    long CreatedAt,
    long UpdatedAt,
    IReadOnlyList<FloorRoomDto> Rooms,
    IReadOnlyList<PlacedFloorDeviceDto> PlacedFloorDevices
);

public sealed record FloorRoomDto(
    Guid Id,
    Guid? LinkedRoomId,
    string? LinkedRoomName,
    string Label,
    IReadOnlyList<FloorPointDto> Polygon,
    string? FillColor
);

public sealed record PlacedFloorDeviceDto(
    Guid Id,
    Guid DeviceId,
    string? DeviceName,
    bool IsOnline,
    bool IsDeleted,
    Guid? FloorRoomId,
    float X,
    float Y
);

public sealed record FloorPointDto(float X, float Y);
