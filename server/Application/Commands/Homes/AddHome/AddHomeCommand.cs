using MediatR;

namespace Application.Commands.Homes.AddHome;

public sealed record AddHomeCommand(string Name, string? Description) : IRequest<Guid>;
