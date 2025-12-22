using Application.Interfaces.Repositories;
using Core.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _context;

    public LocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(Location location)
    {
        _context.Locations.Add(location);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Location>> GetAll()
    {
        return await _context.Locations.ToListAsync();
    }

    public async Task<Location?> GetById(Guid id)
    {
        return await _context.Locations.FindAsync(id);
    }

    public async Task<Location?> GetByIdWithDevicesWithSensorsAndActuators(Guid id)
    {
        return await _context.Locations
            .AsSplitQuery()
            .Include(l => l.Devices)
                .ThenInclude(d => d.Sensors)
            .Include(l => l.Devices)
                .ThenInclude(d => d.Actuators)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public Task Delete(Location location)
    {
        _context.Locations.Remove(location);
        return Task.CompletedTask;
    }
}
