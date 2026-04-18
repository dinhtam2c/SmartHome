using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Devices.GetDeviceDetails;

public sealed class GetDeviceDetailsQueryHandler
    : IRequestHandler<GetDeviceDetailsQuery, DeviceDetailsDto>
{
    private readonly IAppReadDbContext _context;

    public GetDeviceDetailsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<DeviceDetailsDto> Handle(
        GetDeviceDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var device = await _context.Devices
            .AsNoTracking()
            .Include(d => d.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        string? homeName = null;
        string? roomName = null;

        if (device.HomeId.HasValue)
        {
            var home = await _context.Homes
                .AsNoTracking()
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.Id == device.HomeId.Value, cancellationToken);

            homeName = home?.Name;

            if (home is not null && device.RoomId.HasValue)
            {
                roomName = home.Rooms
                    .FirstOrDefault(l => l.Id == device.RoomId.Value)
                    ?.Name;
            }
        }

        var endpointDtos = device.Endpoints
            .Select(endpoint => new DeviceEndpointDetailsDto(
                endpoint.Id,
                endpoint.EndpointId,
                endpoint.Name,
                endpoint.Capabilities.Select(capability =>
                    new DeviceCapabilityDetailsDto(
                        capability.CapabilityId,
                        capability.CapabilityVersion,
                        capability.SupportedOperations,
                        capability.LastReportedAt,
                        device.IsOnline ? capability.State : null
                    )).ToList()))
            .ToList();

        return new DeviceDetailsDto(
            device.Id,
            device.Name,
            device.FirmwareVersion,
            device.ProvisionedAt,
            device.IsOnline,
            device.LastSeenAt,
            device.Uptime,
            device.HomeId,
            homeName,
            device.RoomId,
            roomName,
            endpointDtos
        );
    }
}
