namespace Presentation.Floors;

public sealed record UpdateFloorPlanRoomRequest(
    IReadOnlyCollection<FloorPointRequest>? Polygon,
    string? FillColor
);
