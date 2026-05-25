using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Floors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Floors.GetFloor;

public sealed class GetFloorQueryHandler : IRequestHandler<GetFloorQuery, FloorDto>
{
    private sealed record DeviceLookup(string Name, bool IsOnline);

    private readonly IAppReadDbContext _context;

    public GetFloorQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<FloorDto> Handle(GetFloorQuery request, CancellationToken cancellationToken)
    {
        var homeExists = await _context.Homes
            .AsNoTracking()
            .AnyAsync(home => home.Id == request.HomeId, cancellationToken);

        if (!homeExists)
            throw new HomeNotFoundException(request.HomeId);

        var floor = await _context.Floors
            .AsNoTracking()
            .Include(item => item.Rooms)
            .Include(item => item.PlacedFloorDevices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                item => item.HomeId == request.HomeId && item.Id == request.FloorId,
                cancellationToken);

        if (floor is null)
            throw new FloorNotFoundException(request.FloorId);

        var deviceMap = await LoadDeviceMap(floor.PlacedFloorDevices, cancellationToken);
        var linkedRoomMap = await LoadLinkedRoomMap(
            request.HomeId,
            floor.Rooms,
            cancellationToken);

        return new FloorDto(
            floor.Id,
            floor.HomeId,
            floor.Name,
            floor.SortOrder,
            floor.CanvasWidth,
            floor.CanvasHeight,
            floor.CreatedAt,
            floor.UpdatedAt,
            floor.Rooms
                .OrderBy(room => room.Label)
                .Select(room => new FloorRoomDto(
                    room.Id,
                    room.LinkedRoomId,
                    room.LinkedRoomId.HasValue
                        && linkedRoomMap.TryGetValue(room.LinkedRoomId.Value, out var linkedRoomName)
                            ? linkedRoomName
                            : null,
                    room.Label,
                    room.GetPolygon()
                        .Select(point => new FloorPointDto(point.X, point.Y))
                        .ToList(),
                    room.FillColor))
                .ToList(),
            floor.PlacedFloorDevices
                .OrderBy(placedFloorDevice => placedFloorDevice.DeviceId)
                .Select(placedFloorDevice =>
                {
                    var hasDevice = deviceMap.TryGetValue(placedFloorDevice.DeviceId, out var deviceLookup);

                    return new PlacedFloorDeviceDto(
                        placedFloorDevice.Id,
                        placedFloorDevice.DeviceId,
                        hasDevice ? deviceLookup!.Name : null,
                        hasDevice && deviceLookup!.IsOnline,
                        !hasDevice,
                        placedFloorDevice.FloorRoomId,
                        placedFloorDevice.X,
                        placedFloorDevice.Y);
                })
                .ToList());
    }

    private async Task<Dictionary<Guid, DeviceLookup>> LoadDeviceMap(
        IEnumerable<PlacedFloorDevice> placedFloorDevices,
        CancellationToken cancellationToken)
    {
        var deviceIds = placedFloorDevices
            .Select(placedFloorDevice => placedFloorDevice.DeviceId)
            .Distinct()
            .ToList();

        if (deviceIds.Count == 0)
            return [];

        return await _context.Devices
            .AsNoTracking()
            .Where(device => deviceIds.Contains(device.Id))
            .Select(device => new DeviceLookupRow(device.Id, device.Name, device.IsOnline))
            .ToDictionaryAsync(
                device => device.Id,
                device => new DeviceLookup(device.Name, device.IsOnline),
                cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> LoadLinkedRoomMap(
        Guid homeId,
        IEnumerable<FloorRoom> floorRooms,
        CancellationToken cancellationToken)
    {
        var linkedRoomIds = floorRooms
            .Where(room => room.LinkedRoomId.HasValue)
            .Select(room => room.LinkedRoomId!.Value)
            .Distinct()
            .ToList();

        if (linkedRoomIds.Count == 0)
            return [];

        return await _context.Homes
            .AsNoTracking()
            .Where(home => home.Id == homeId)
            .SelectMany(home => home.Rooms)
            .Where(room => linkedRoomIds.Contains(room.Id))
            .Select(room => new LinkedRoomLookupRow(room.Id, room.Name))
            .ToDictionaryAsync(room => room.Id, room => room.Name, cancellationToken);
    }

    private sealed record DeviceLookupRow(Guid Id, string Name, bool IsOnline);

    private sealed record LinkedRoomLookupRow(Guid Id, string Name);
}
