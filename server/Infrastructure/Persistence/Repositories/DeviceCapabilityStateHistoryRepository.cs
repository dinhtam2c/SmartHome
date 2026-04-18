using Core.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DeviceCapabilityStateHistoryRepository : IDeviceCapabilityStateHistoryRepository
{
    private readonly AppDbContext _context;

    public DeviceCapabilityStateHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(DeviceCapabilityStateHistory history)
    {
        _context.DeviceCapabilityStateHistories.Add(history);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<DeviceCapabilityStateHistory>> GetByCapability(
        Guid deviceId,
        string capabilityId,
        string endpointId,
        long? from,
        long? to,
        int limit)
    {
        var query = _context.DeviceCapabilityStateHistories
            .Where(h => h.DeviceId == deviceId
                && h.CapabilityId == capabilityId
                && h.EndpointId.Equals(endpointId));

        if (from.HasValue)
            query = query.Where(h => h.ReportedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(h => h.ReportedAt <= to.Value);

        return await query
            .OrderByDescending(h => h.ReportedAt)
            .Take(limit)
            .ToListAsync();
    }
}
