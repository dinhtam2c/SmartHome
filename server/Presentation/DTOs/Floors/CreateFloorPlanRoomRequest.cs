namespace Presentation.Floors;

public sealed record CreateFloorPlanRoomRequest(
    Guid RoomId,
    IReadOnlyCollection<FloorPointRequest>? Polygon,
    string? FillColor
);
