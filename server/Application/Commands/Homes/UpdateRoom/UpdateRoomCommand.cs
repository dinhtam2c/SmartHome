using MediatR;

namespace Application.Commands.Homes.UpdateRoom;

public sealed record UpdateRoomCommand(Guid HomeId, Guid RoomId, string Name, string? Description) : IRequest;
