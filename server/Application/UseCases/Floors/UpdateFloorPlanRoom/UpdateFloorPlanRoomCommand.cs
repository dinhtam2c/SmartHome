using MediatR;

namespace Application.UseCases.Floors.UpdateFloorPlanRoom;

public sealed record UpdateFloorPlanRoomCommand(
    Guid HomeId,
    Guid FloorId,
    Guid FloorPlanRoomId,
    IReadOnlyCollection<FloorPointModel>? Polygon,
    string? FillColor
) : IRequest;
