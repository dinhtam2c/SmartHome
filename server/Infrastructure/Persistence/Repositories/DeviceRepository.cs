using Core.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _context;

    public DeviceRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(Device device, CancellationToken ct = default)
    {
        _context.Devices.Add(device);
        return Task.CompletedTask;
    }

    public async Task<Device?> GetById(Guid id, CancellationToken ct = default)
    {
        return await _context.Devices
            .Include(d => d.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<Device?> GetByMacAddress(string macAddress, CancellationToken ct = default)
    {
        return await _context.Devices
            .Include(d => d.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.MacAddress == macAddress, ct);
    }

    public async Task<Device?> GetByProvisionCode(string provisionCode, CancellationToken ct = default)
    {
        return await _context.Devices
            .Include(d => d.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.ProvisionCode == provisionCode, ct);
    }

    public void Remove(Device device)
    {
        _context.Devices.Remove(device);
    }
}
