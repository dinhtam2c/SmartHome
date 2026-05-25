using MediatR;

namespace Application.UseCases.Floors.RemoveFloorPlanRoom;

public sealed record RemoveFloorPlanRoomCommand(
    Guid HomeId,
    Guid FloorId,
    Guid FloorPlanRoomId
) : IRequest;
