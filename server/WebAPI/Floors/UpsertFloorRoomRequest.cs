namespace WebAPI.Floors;

public sealed record UpsertFloorRoomRequest(
    Guid? LinkedRoomId,
    string Label,
    IReadOnlyList<FloorPointRequest>? Polygon,
    string? FillColor
);
