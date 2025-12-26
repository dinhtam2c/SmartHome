using Application.Interfaces.Repositories;
using Core.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class GatewayRepository : IGatewayRepository
{
    private readonly AppDbContext _context;

    public GatewayRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(Gateway gateway)
    {
        _context.Gateways.Add(gateway);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Gateway>> GetAllWithHome()
    {
        return await _context.Gateways
            .Include(g => g.Home)
            .ToListAsync();
    }

    public async Task<Gateway?> GetById(Guid id)
    {
        return await _context.Gateways.FindAsync(id);
    }

    public async Task<Gateway?> GetByIdWithHome(Guid id)
    {
        return await _context.Gateways
            .Include(g => g.Home)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Gateway?> GetByIdWithDevices(Guid id)
    {
        return await _context.Gateways
            .Include(g => g.Devices)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Gateway?> GetByMac(string mac)
    {
        return await _context.Gateways.FirstOrDefaultAsync(g => g.Mac == mac);
    }
}
