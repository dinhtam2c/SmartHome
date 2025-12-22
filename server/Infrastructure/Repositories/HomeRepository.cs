using Application.Interfaces.Repositories;
using Core.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class HomeRepository : IHomeRepository
{
    private readonly AppDbContext _context;

    public HomeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(Home home)
    {
        _context.Homes.Add(home);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Home>> GetAll()
    {
        return await _context.Homes.ToListAsync();
    }

    public async Task<Home?> GetById(Guid id)
    {
        return await _context.Homes.FindAsync(id);
    }

    public async Task<Home?> GetByIdWithLocations(Guid id)
    {
        return await _context.Homes
            .Include(h => h.Locations)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public Task Delete(Home home)
    {
        _context.Homes.Remove(home);
        return Task.CompletedTask;
    }
}
