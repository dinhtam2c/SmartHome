using Application.Interfaces.Repositories;
using Core.Entities;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class SensorRepository : ISensorRepository
{
    private readonly AppDbContext _context;

    public SensorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Sensor?> GetById(Guid id)
    {
        return await _context.Sensors.FindAsync(id);
    }
}
