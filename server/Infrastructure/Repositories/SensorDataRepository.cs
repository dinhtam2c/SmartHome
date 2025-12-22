using Application.Interfaces.Repositories;
using Core.Entities;
using Infrastructure.Persistence;

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
}
