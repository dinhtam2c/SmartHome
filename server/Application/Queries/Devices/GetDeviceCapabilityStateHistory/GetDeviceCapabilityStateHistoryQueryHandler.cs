using Application.Common.Data;
using Application.Exceptions;
using Application.Queries.Devices.GetDeviceCommandExecutions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Devices.GetDeviceCapabilityStateHistory;

public sealed class GetDeviceCapabilityStateHistoryQueryHandler
    : IRequestHandler<GetDeviceCapabilityStateHistoryQuery, PagedResult<DeviceCapabilityStateHistoryDto>>
{
    private readonly IAppReadDbContext _context;

    public GetDeviceCapabilityStateHistoryQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<DeviceCapabilityStateHistoryDto>> Handle(
        GetDeviceCapabilityStateHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var deviceExists = await _context.Devices
            .AsNoTracking()
            .AnyAsync(d => d.Id == request.DeviceId, cancellationToken);

        if (!deviceExists)
            throw new DeviceNotFoundException(request.DeviceId);

        var query = _context.DeviceCapabilityStateHistories
            .AsNoTracking()
            .Where(h => h.DeviceId == request.DeviceId);

        if (!string.IsNullOrWhiteSpace(request.EndpointId))
            query = query.Where(h =>
                h.EndpointId.ToLower() == request.EndpointId.ToLower());

        if (!string.IsNullOrWhiteSpace(request.CapabilityId))
            query = query.Where(h =>
                h.CapabilityId.ToLower() == request.CapabilityId.ToLower());

        if (request.FromReportedAt.HasValue)
            query = query.Where(h => h.ReportedAt >= request.FromReportedAt.Value);

        if (request.ToReportedAt.HasValue)
            query = query.Where(h => h.ReportedAt <= request.ToReportedAt.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var items = await query
            .OrderByDescending(h => h.ReportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new DeviceCapabilityStateHistoryDto(
                h.Id,
                h.DeviceId,
                h.CapabilityId,
                h.EndpointId,
                h.StatePayload,
                h.ReportedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<DeviceCapabilityStateHistoryDto>(items, page, pageSize, totalCount);
    }
}
