using MediatR;

namespace Application.UseCases.Floors.CreateFloorPlanRoom;

public sealed record CreateFloorPlanRoomCommand(
    Guid HomeId,
    Guid FloorId,
    Guid RoomId,
    IReadOnlyCollection<FloorPointModel>? Polygon,
    string? FillColor
) : IRequest<Guid>;
