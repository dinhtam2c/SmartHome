using System.Text.Json;
using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Homes.GetHomeDetails;

public class GetHomeDetailsQueryHandler : IRequestHandler<GetHomeDetailsQuery, HomeDetailsDto>
{
    private readonly IAppReadDbContext _context;

    public GetHomeDetailsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<HomeDetailsDto> Handle(GetHomeDetailsQuery request, CancellationToken cancellationToken)
    {
        var home = await _context.Homes
            .AsNoTracking()
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync(h => h.Id == request.HomeId, cancellationToken)
            ?? throw new HomeNotFoundException(request.HomeId);

        var devices = await _context.Devices
            .AsNoTracking()
            .Where(d => d.HomeId == home.Id)
            .Include(d => d.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var roomLookup = devices
            .Where(d => d.RoomId.HasValue)
            .GroupBy(d => d.RoomId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var roomDtos = home.Rooms.Select(room =>
        {
            roomLookup.TryGetValue(room.Id, out var roomDevices);
            roomDevices ??= [];

            var onlineCount = roomDevices.Count(d => d.IsOnline);

            var temperatures = roomDevices
                .Where(d => d.IsOnline)
                .SelectMany(d => d.Capabilities)
                .Where(c =>
                    IsCapabilityKind(c.CapabilityId, "temperature"))
                .Select(c =>
                {
                    if (c.State.TryGetValue("value", out var val) &&
                        val is JsonElement json &&
                        json.ValueKind == JsonValueKind.Number &&
                        json.TryGetDouble(out var dVal))
                    {
                        return (double?)dVal;
                    }

                    return null;
                })
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            double? avgTemp = temperatures.Any()
                ? temperatures.Average()
                : null;

            var humidities = roomDevices
                .Where(d => d.IsOnline)
                .SelectMany(d => d.Capabilities)
                .Where(c =>
                    IsCapabilityKind(c.CapabilityId, "humidity"))
                .Select(c =>
                {
                    if (c.State.TryGetValue("value", out var val) &&
                        val is JsonElement json &&
                        json.ValueKind == JsonValueKind.Number &&
                        json.TryGetDouble(out var dVal))
                    {
                        return (double?)dVal;
                    }

                    return null;
                })
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            double? avgHumidity = humidities.Any()
                ? humidities.Average()
                : null;

            return new RoomOverviewDto(
                room.Id,
                room.Name,
                room.Description,
                roomDevices.Count,
                onlineCount,
                avgTemp,
                avgHumidity
            );
        }).ToList();

        var unassignedDeviceDtos = devices
            .Where(device => !device.RoomId.HasValue)
            .OrderBy(device => device.Name)
            .Select(device => new HomeUnassignedDeviceOverviewDto(
                device.Id,
                device.Name,
                device.IsOnline,
                device.Endpoints
                    .OrderBy(endpoint => endpoint.EndpointId)
                    .Select(endpoint => new HomeUnassignedDeviceEndpointOverviewDto(
                        endpoint.EndpointId,
                        endpoint.Name,
                        endpoint.Capabilities
                            .OrderBy(capability => capability.CapabilityId)
                            .Select(capability => new HomeUnassignedCapabilityOverviewDto(
                                capability.CapabilityId,
                                capability.CapabilityVersion,
                                capability.SupportedOperations,
                                capability.LastReportedAt,
                                device.IsOnline ? capability.State : null))
                            .ToList()))
                    .ToList()))
            .ToList();

        var totalDevices = devices.Count;
        var onlineDevices = devices.Count(d => d.IsOnline);

        var sceneSummaries = await _context.Scenes
            .AsNoTracking()
            .Where(scene => scene.HomeId == home.Id)
            .OrderBy(scene => scene.Name)
            .Select(scene => new HomeSceneSummaryDto(
                scene.Id,
                scene.Name,
                scene.IsEnabled))
            .ToListAsync(cancellationToken);

        return new HomeDetailsDto(
            home.Id,
            home.Name,
            home.Description,
            home.CreatedAt,
            home.Rooms.Count,
            totalDevices,
            onlineDevices,
            roomDtos,
            unassignedDeviceDtos,
            sceneSummaries
        );
    }

    private static bool IsCapabilityKind(string capabilityId, string kind)
    {
        return capabilityId.Contains(kind, StringComparison.OrdinalIgnoreCase);
    }
}
