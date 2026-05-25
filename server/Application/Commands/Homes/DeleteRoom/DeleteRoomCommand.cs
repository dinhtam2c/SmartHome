using MediatR;

namespace Application.Commands.Homes.DeleteRoom;

public sealed record DeleteRoomCommand(Guid HomeId, Guid RoomId) : IRequest;
