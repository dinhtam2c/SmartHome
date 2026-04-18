using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Homes.GetHomeDevices;

public sealed class GetHomeDevicesQueryHandler
    : IRequestHandler<GetHomeDevicesQuery, IReadOnlyList<HomeDeviceDto>>
{
    private readonly IAppReadDbContext _context;

    public GetHomeDevicesQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HomeDeviceDto>> Handle(
        GetHomeDevicesQuery request,
        CancellationToken cancellationToken)
    {
        var home = await _context.Homes
            .AsNoTracking()
            .Include(item => item.Rooms)
            .FirstOrDefaultAsync(item => item.Id == request.HomeId, cancellationToken)
            ?? throw new HomeNotFoundException(request.HomeId);

        if (request.RoomId.HasValue
            && home.Rooms.All(room => room.Id != request.RoomId.Value))
        {
            throw new RoomNotFoundException(request.RoomId.Value);
        }

        var devicesQuery = _context.Devices
            .AsNoTracking()
            .Where(device => device.HomeId == request.HomeId)
            .Include(device => device.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery();

        if (request.RoomId.HasValue)
        {
            devicesQuery = devicesQuery.Where(device => device.RoomId == request.RoomId.Value);
        }

        var devices = await devicesQuery
            .OrderBy(device => device.Name)
            .ToListAsync(cancellationToken);

        var roomLookup = home.Rooms
            .ToDictionary(room => room.Id, room => room.Name);

        var result = devices
            .Select(device =>
            {
                var roomName = device.RoomId.HasValue
                    && roomLookup.TryGetValue(device.RoomId.Value, out var value)
                        ? value
                        : null;

                var endpointDtos = device.Endpoints
                    .OrderBy(endpoint => endpoint.EndpointId)
                    .Select(endpoint => new HomeDeviceEndpointDto(
                        endpoint.EndpointId,
                        endpoint.Name,
                        endpoint.Capabilities
                            .OrderBy(capability => capability.CapabilityId)
                            .Select(capability => new HomeDeviceCapabilityDto(
                                capability.CapabilityId,
                                capability.CapabilityVersion,
                                capability.SupportedOperations,
                                capability.LastReportedAt,
                                capability.State))
                            .ToList()))
                    .ToList();

                return new HomeDeviceDto(
                    device.Id,
                    device.Name,
                    device.FirmwareVersion,
                    device.IsOnline,
                    device.ProvisionedAt ?? 0,
                    device.LastSeenAt,
                    device.Uptime,
                    device.RoomId,
                    roomName,
                    endpointDtos);
            })
            .ToList();

        return result;
    }
}
