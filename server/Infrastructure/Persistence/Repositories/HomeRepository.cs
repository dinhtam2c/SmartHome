using Core.Domain.Homes;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class HomeRepository : IHomeRepository
{
    private readonly AppDbContext _context;

    public HomeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Home?> GetById(Guid id, CancellationToken ct = default)
    {
        return await _context.Homes
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public Task Add(Home home, CancellationToken ct = default)
    {
        _context.Homes.Add(home);
        return Task.CompletedTask;
    }

    public void Remove(Home home)
    {
        _context.Homes.Remove(home);
    }
}
