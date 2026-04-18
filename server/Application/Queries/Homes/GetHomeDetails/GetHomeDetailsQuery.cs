using MediatR;

namespace Application.Queries.Homes.GetHomeDetails;

public record GetHomeDetailsQuery(Guid HomeId) : IRequest<HomeDetailsDto>;
