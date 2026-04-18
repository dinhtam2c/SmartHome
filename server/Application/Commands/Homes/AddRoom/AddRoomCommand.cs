using MediatR;

namespace Application.Commands.Homes.AddRoom;

public sealed record AddRoomCommand(Guid HomeId, string Name, string? Description) : IRequest<Guid>;
