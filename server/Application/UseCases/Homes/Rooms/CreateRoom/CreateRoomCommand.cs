using MediatR;

namespace Application.UseCases.Homes.Rooms.CreateRoom;

public sealed record CreateRoomCommand(Guid HomeId, string Name, string? Description) : IRequest<Guid>;
