using MediatR;

namespace Application.UseCases.Homes.DeleteHome;

public sealed record DeleteHomeCommand(Guid HomeId) : IRequest;
