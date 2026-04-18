using System.Globalization;
using System.Text.Json;
using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Rooms.GetRoomDetails;

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
            .Select(c => TryReadStateNumber(c.Capability.State))
            .Where(v => v != null)
            .Select(v => v!.Value)
            .ToList();

        var allHumidityValues = capabilityEntries
            .Where(c =>
                c.Device.IsOnline
                && IsCapabilityKind(c.Capability.CapabilityId, "humidity"))
            .Select(c => TryReadStateNumber(c.Capability.State))
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

    private static double? TryReadStateNumber(IReadOnlyDictionary<string, object?> state)
    {
        if (state.TryGetValue("value", out var value))
            return TryConvertToDouble(value);

        if (state.Count == 1)
            return TryConvertToDouble(state.Values.FirstOrDefault());

        return null;
    }

    private static double? TryConvertToDouble(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case JsonElement json when json.ValueKind == JsonValueKind.Number && json.TryGetDouble(out var d):
                return d;
            case JsonElement json when json.ValueKind == JsonValueKind.String
                                       && double.TryParse(
                                           json.GetString(),
                                           NumberStyles.Float,
                                           CultureInfo.InvariantCulture,
                                           out var parsedFromJson):
                return parsedFromJson;
            case byte v:
                return v;
            case sbyte v:
                return v;
            case short v:
                return v;
            case ushort v:
                return v;
            case int v:
                return v;
            case uint v:
                return v;
            case long v:
                return v;
            case ulong v:
                return v;
            case float v:
                return v;
            case double v:
                return v;
            case decimal v:
                return (double)v;
            case string text when double.TryParse(
                                  text,
                                  NumberStyles.Float,
                                  CultureInfo.InvariantCulture,
                                  out var parsed):
                return parsed;
            default:
                return null;
        }
    }
}
