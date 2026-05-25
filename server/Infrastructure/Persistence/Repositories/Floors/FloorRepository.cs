using Domain.Models.Floors;
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

    public Task<List<Floor>> ListByHomeId(Guid homeId, CancellationToken ct = default)
    {
        return BaseQuery()
            .Where(floor => floor.HomeId == homeId)
            .OrderBy(floor => floor.SortOrder)
            .ToListAsync(ct);
    }

    public Task<Floor?> GetById(Guid id, CancellationToken ct = default)
    {
        return BaseQuery().FirstOrDefaultAsync(floor => floor.Id == id, ct);
    }

    public Task<Floor?> GetByRoomId(Guid roomId, CancellationToken ct = default)
    {
        return BaseQuery()
            .FirstOrDefaultAsync(
                floor => floor.FloorPlanRooms.Any(room => room.RoomId == roomId),
                ct);
    }

    public Task<Floor?> GetByDeviceId(Guid deviceId, CancellationToken ct = default)
    {
        return BaseQuery()
            .FirstOrDefaultAsync(
                floor => floor.DevicePlacements.Any(placement => placement.DeviceId == deviceId),
                ct);
    }

    public void Remove(Floor floor)
    {
        _context.Floors.Remove(floor);
    }

    private IQueryable<Floor> BaseQuery()
    {
        return _context.Floors
            .Include(floor => floor.FloorPlanRooms)
            .Include(floor => floor.DevicePlacements)
            .AsSplitQuery();
    }
}
