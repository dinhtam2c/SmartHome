using MediatR;

namespace Application.Commands.Homes.DeleteHome;

public sealed record DeleteHomeCommand(Guid HomeId) : IRequest;
