using Application.Interfaces.Repositories;
using Core.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SensorDataRepository : ISensorDataRepository
{
    private readonly AppDbContext _context;

    public SensorDataRepository(AppDbContext appDbContext)
    {
        _context = appDbContext;
    }

    public Task AddRange(IEnumerable<SensorData> sensorData)
    {
        _context.SensorData.AddRange(sensorData);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<SensorData>> GetAllWithSensor()
    {
        return await _context.SensorData
            .Include(sd => sd.Sensor)
            .ToListAsync();
    }
}
