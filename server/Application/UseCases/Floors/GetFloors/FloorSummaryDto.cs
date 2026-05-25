namespace Application.UseCases.Floors.GetFloors;

public sealed record FloorSummaryDto(
    Guid Id,
    Guid HomeId,
    string Name,
    int SortOrder,
    int CanvasWidth,
    int CanvasHeight,
    long CreatedAt,
    long UpdatedAt,
    int FloorPlanRoomCount,
    int DevicePlacementCount,
    IReadOnlyList<Guid> MappedRoomIds,
    IReadOnlyList<Guid> PlacedDeviceIds
);
