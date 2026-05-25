using MediatR;

namespace Application.UseCases.Homes.GetHomes;

public sealed record GetHomesQuery : IRequest<IReadOnlyList<HomeListItemDto>>;
