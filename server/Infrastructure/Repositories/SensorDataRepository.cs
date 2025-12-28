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

    public async Task<Dictionary<Guid, SensorData>> GetLatestBySensorIds(IEnumerable<Guid> sensorIds)
    {
        return await _context.SensorData
            .Where(sd => sd.SensorId != null && sensorIds.Contains(sd.SensorId.Value))
            .GroupBy(sd => sd.SensorId!.Value)
            .Select(g => g
                .OrderByDescending(sd => sd.Timestamp)
                .First())
            .ToDictionaryAsync(sd => sd.SensorId!.Value, sd => sd);
    }
}
