using MediatR;

namespace Application.Commands.Floors.RemoveFloorRoom;

public sealed record RemoveFloorRoomCommand(Guid HomeId, Guid FloorId, Guid RoomId) : IRequest;
