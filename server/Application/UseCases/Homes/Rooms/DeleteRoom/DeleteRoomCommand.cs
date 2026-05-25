using MediatR;

namespace Application.UseCases.Homes.Rooms.DeleteRoom;

public sealed record DeleteRoomCommand(Guid HomeId, Guid RoomId) : IRequest;
