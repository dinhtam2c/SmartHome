using Core.Domain.Floors;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Floors;

public class FloorRepository : IFloorRepository
{
    private readonly AppDbContext _context;

    public FloorRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(Floor floor, CancellationToken ct = default)
    {
        _context.Floors.Add(floor);
        return Task.CompletedTask;
    }

    public async Task<int> GetNextSortOrder(Guid homeId, CancellationToken ct = default)
    {
        var maxSortOrder = await _context.Floors
            .Where(floor => floor.HomeId == homeId)
            .Select(floor => (int?)floor.SortOrder)
            .MaxAsync(ct);

        return (maxSortOrder ?? 0) + 1;
    }

    public async Task<List<Floor>> ListByHomeId(Guid homeId, CancellationToken ct = default)
    {
        return await _context.Floors
            .Include(floor => floor.Rooms)
            .Include(floor => floor.PlacedFloorDevices)
            .AsSplitQuery()
            .Where(floor => floor.HomeId == homeId)
            .OrderBy(floor => floor.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<bool> HasPlacedDevice(Guid homeId, Guid deviceId, CancellationToken ct = default)
    {
        return await _context.Floors
            .AsNoTracking()
            .Where(floor => floor.HomeId == homeId)
            .SelectMany(floor => floor.PlacedFloorDevices)
            .AnyAsync(placedFloorDevice => placedFloorDevice.DeviceId == deviceId, ct);
    }

    public async Task<Floor?> GetById(Guid id, CancellationToken ct = default)
    {
        return await _context.Floors
            .Include(floor => floor.Rooms)
            .Include(floor => floor.PlacedFloorDevices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(floor => floor.Id == id, ct);
    }

    public void Remove(Floor floor)
    {
        _context.Floors.Remove(floor);
    }
}
