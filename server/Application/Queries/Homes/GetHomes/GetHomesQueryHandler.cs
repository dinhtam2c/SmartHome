using Application.Common.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Homes.GetHomes;

public class GetHomesQueryHandler : IRequestHandler<GetHomesQuery, IReadOnlyList<HomeListItemDto>>
{
    private readonly IAppReadDbContext _context;

    public GetHomesQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HomeListItemDto>> Handle(GetHomesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Homes
            .AsNoTracking()
            .OrderBy(h => h.CreatedAt)
            .Select(h => new HomeListItemDto(
                h.Id,
                h.Name,
                h.Description,
                h.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
