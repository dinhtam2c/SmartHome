using Application.Ports.Persistence;
using Domain.Models.Devices;
using Domain.Models.Floors;
using Microsoft.EntityFrameworkCore;

namespace Application.BusinessServices.Floors;

public sealed class FloorPlanConsistencyService
{
    private readonly IFloorRepository _floorRepository;
    private readonly IAppReadDbContext _readContext;

    public FloorPlanConsistencyService(
        IFloorRepository floorRepository,
        IAppReadDbContext readContext)
    {
        _floorRepository = floorRepository;
        _readContext = readContext;
    }

    public void ApplyPlacementRoom(Floor floor, Device device, float x, float y)
    {
        device.AssignRoom(floor.ResolveRoomId(x, y));
    }

    public async Task ReconcileDeviceRoom(
        Device device,
        Guid? roomId,
        CancellationToken cancellationToken)
    {
        device.AssignRoom(roomId);

        var floor = await _floorRepository.GetByDeviceId(device.Id, cancellationToken);
        var placement = floor?.DevicePlacements.FirstOrDefault(item => item.DeviceId == device.Id);
        if (floor is not null
            && placement is not null
            && !floor.PlacementMatchesRoom(placement, roomId))
        {
            floor.RemoveDevicePlacement(placement.Id);
        }
    }

    public async Task ReconcileRoomGeometry(
        Floor floor,
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var assignedDeviceIds = (await _readContext.Devices
            .AsNoTracking()
            .Where(device => device.RoomId == roomId)
            .Select(device => device.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var inconsistentDeviceIds = floor.DevicePlacements
            .Where(placement => assignedDeviceIds.Contains(placement.DeviceId))
            .Where(placement => !floor.PlacementMatchesRoom(placement, roomId))
            .Select(placement => placement.DeviceId)
            .ToList();

        floor.RemoveDevicePlacements(inconsistentDeviceIds);
    }

    public async Task RemoveRoomPlacements(
        Floor floor,
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var deviceIds = await _readContext.Devices
            .AsNoTracking()
            .Where(device => device.RoomId == roomId)
            .Select(device => device.Id)
            .ToListAsync(cancellationToken);
        floor.RemoveDevicePlacements(deviceIds);
    }
}
