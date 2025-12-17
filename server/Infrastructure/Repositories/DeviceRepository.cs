using Application.Interfaces.Repositories;
using Core.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _context;

    public DeviceRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task Add(Device device)
    {
        _context.Devices.Add(device);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Device>> GetAllWithGatewayAndHomeAndLocation()
    {
        return await _context.Devices
            .AsSplitQuery()
            .Include(d => d.Gateway)
            .ThenInclude(g => g!.Home)
            .Include(d => d.Location)
            .ToListAsync();
    }

    public async Task<Device?> GetById(Guid id)
    {
        return await _context.Devices.FindAsync(id);
    }

    public async Task<IEnumerable<Device>> GetByIdsWithLocationAndSensors(IEnumerable<Guid> ids)
    {
        return await _context.Devices
            .Include(d => d.Location)
            .Include(d => d.Sensors)
            .Where(d => ids.Contains(d.Id))
            .ToListAsync();
    }

    public async Task<Device?> GetByIdWithGatewayAndLocationAndCapabilities(Guid id)
    {
        return await _context.Devices
            .AsSplitQuery()
            .Include(d => d.Gateway)
            .Include(d => d.Location)
            .Include(d => d.Sensors)
            .Include(d => d.Actuators)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Device?> GetByIdWithActuators(Guid id)
    {
        return await _context.Devices
            .Include(d => d.Actuators)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Device?> GetByIdentifierWithCapabilities(string identifier)
    {
        return await _context.Devices
            .AsSplitQuery()
            .Include(d => d.Sensors)
            .Include(d => d.Actuators)
            .FirstOrDefaultAsync(d => d.Identifier == identifier);
    }
}
