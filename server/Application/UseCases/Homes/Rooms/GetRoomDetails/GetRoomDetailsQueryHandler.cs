using Application.Common.Capabilities;
using Application.Ports.Persistence;
using Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Homes.Rooms.GetRoomDetails;

public class GetRoomDetailsQueryHandler : IRequestHandler<GetRoomDetailsQuery, RoomDetailsDto>
{
    private readonly IAppReadDbContext _context;

    public GetRoomDetailsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<RoomDetailsDto> Handle(GetRoomDetailsQuery request, CancellationToken cancellationToken)
    {
        var room = await _context.Homes
            .AsNoTracking()
            .Where(x => x.Id == request.HomeId)
            .SelectMany(h => h.Rooms)
            .Where(l => l.Id == request.RoomId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RoomNotFoundException(request.RoomId);

        var devices = await _context.Devices
            .AsNoTracking()
            .Where(d => d.RoomId == room.Id)
            .Include(d => d.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var capabilityEntries = devices
            .SelectMany(device => device.Endpoints.SelectMany(endpoint => endpoint.Capabilities.Select(capability => new
            {
                Device = device,
                Capability = capability
            })))
            .ToList();

        var allTempValues = capabilityEntries
            .Where(c =>
                c.Device.IsOnline
                && IsCapabilityKind(c.Capability.CapabilityId, "temperature"))
            .Select(c => CapabilityStateReader.TryReadNumber(c.Capability.State))
            .Where(v => v != null)
            .Select(v => v!.Value)
            .ToList();

        var allHumidityValues = capabilityEntries
            .Where(c =>
                c.Device.IsOnline
                && IsCapabilityKind(c.Capability.CapabilityId, "humidity"))
            .Select(c => CapabilityStateReader.TryReadNumber(c.Capability.State))
            .Where(v => v != null)
            .Select(v => v!.Value)
            .ToList();

        var deviceDtos = devices.Select(d =>
        {
            var endpointDtos = d.Endpoints
                .Select(endpoint => new DeviceEndpointOverviewDto(
                    endpoint.EndpointId,
                    endpoint.Name,
                    endpoint.Capabilities.Select(capability =>
                        new CapabilityOverviewDto(
                            capability.CapabilityId,
                            capability.CapabilityVersion,
                            capability.SupportedOperations,
                            capability.LastReportedAt,
                            d.IsOnline ? capability.State : null)).ToList()))
                .ToList();

            return new DeviceOverviewDto(
                d.Id,
                d.Name,
                d.Category,
                d.IsOnline,
                endpointDtos
            );
        });

        return new RoomDetailsDto(
            room.Id,
            room.Name,
            room.Description,
            room.CreatedAt,
            devices.Count,
            devices.Count(d => d.IsOnline),
            allTempValues.Any() ? allTempValues.Average() : null,
            allHumidityValues.Any() ? allHumidityValues.Average() : null,
            deviceDtos
        );
    }

    private static bool IsCapabilityKind(string capabilityId, string kind)
    {
        return capabilityId.Contains(kind, StringComparison.OrdinalIgnoreCase);
    }

}
