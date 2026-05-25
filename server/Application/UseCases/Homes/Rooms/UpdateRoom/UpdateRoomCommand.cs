using MediatR;

namespace Application.UseCases.Homes.Rooms.UpdateRoom;

public sealed record UpdateRoomCommand(Guid HomeId, Guid RoomId, string Name, string? Description) : IRequest;
