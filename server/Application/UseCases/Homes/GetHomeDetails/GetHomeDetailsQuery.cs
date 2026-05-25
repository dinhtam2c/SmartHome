using MediatR;

namespace Application.UseCases.Homes.GetHomeDetails;

public record GetHomeDetailsQuery(Guid HomeId) : IRequest<HomeDetailsDto>;
