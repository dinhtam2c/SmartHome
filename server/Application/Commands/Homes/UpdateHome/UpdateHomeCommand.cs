using MediatR;

namespace Application.Commands.Homes.UpdateHome;

public sealed record UpdateHomeCommand(Guid HomeId, string Name, string? Description) : IRequest;
