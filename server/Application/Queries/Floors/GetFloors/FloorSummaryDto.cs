namespace Application.Queries.Floors.GetFloors;

public sealed record FloorSummaryDto(
    Guid Id,
    Guid HomeId,
    string Name,
    int SortOrder,
    int CanvasWidth,
    int CanvasHeight,
    long CreatedAt,
    long UpdatedAt,
    int RoomCount,
    int PlacedDeviceCount,
    IReadOnlyList<Guid> PlacedDeviceIds
);
