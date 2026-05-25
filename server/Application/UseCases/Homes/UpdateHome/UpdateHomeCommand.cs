using MediatR;

namespace Application.UseCases.Homes.UpdateHome;

public sealed record UpdateHomeCommand(Guid HomeId, string Name, string? Description) : IRequest;
