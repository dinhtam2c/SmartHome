using MediatR;

namespace Application.Queries.Homes.GetHomes;

public sealed record GetHomesQuery : IRequest<IReadOnlyList<HomeListItemDto>>;
