using MediatR;

namespace Application.UseCases.Homes.CreateHome;

public sealed record CreateHomeCommand(string Name, string? Description) : IRequest<Guid>;
