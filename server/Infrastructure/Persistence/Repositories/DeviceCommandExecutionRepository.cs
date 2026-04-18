using Core.Domain.DeviceCommandExecutions;
using Core.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DeviceCommandExecutionRepository : IDeviceCommandExecutionRepository
{
    private readonly AppDbContext _context;

    public DeviceCommandExecutionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(DeviceCommandExecution execution)
    {
        _context.DeviceCommandExecutions.Add(execution);
        return Task.CompletedTask;
    }

    public async Task<DeviceCommandExecution?> GetByCorrelation(Guid deviceId, string correlationId)
    {
        return await _context.DeviceCommandExecutions
            .FirstOrDefaultAsync(e => e.DeviceId == deviceId && e.CorrelationId == correlationId);
    }

    public async Task<IEnumerable<DeviceCommandExecution>> GetPendingOlderThan(long unixCutoff, int limit)
    {
        return await _context.DeviceCommandExecutions
            .Where(e => e.Status == CommandLifecycleStatus.Pending && e.RequestedAt <= unixCutoff)
            .OrderBy(e => e.RequestedAt)
            .Take(limit)
            .ToListAsync();
    }
}
