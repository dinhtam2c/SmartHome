using MediatR;

namespace Application.Commands.Floors.UpsertFloorRoom;

public sealed record UpsertFloorRoomCommand(
    Guid HomeId,
    Guid FloorId,
    Guid? RoomId,
    Guid? LinkedRoomId,
    string Label,
    IReadOnlyCollection<FloorPointModel>? Polygon,
    string? FillColor
) : IRequest<Guid>;
