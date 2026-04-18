using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Devices.GetDeviceCommandExecutions;

public sealed class GetDeviceCommandExecutionsQueryHandler
    : IRequestHandler<GetDeviceCommandExecutionsQuery, PagedResult<DeviceCommandExecutionDto>>
{
    private readonly IAppReadDbContext _context;

    public GetDeviceCommandExecutionsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<DeviceCommandExecutionDto>> Handle(
        GetDeviceCommandExecutionsQuery request,
        CancellationToken cancellationToken)
    {
        var deviceExists = await _context.Devices
            .AsNoTracking()
            .AnyAsync(d => d.Id == request.DeviceId, cancellationToken);

        if (!deviceExists)
            throw new DeviceNotFoundException(request.DeviceId);

        var query = _context.DeviceCommandExecutions
            .AsNoTracking()
            .Where(e => e.DeviceId == request.DeviceId);

        if (!string.IsNullOrWhiteSpace(request.EndpointId))
            query = query.Where(e =>
                e.EndpointId.ToLower() == request.EndpointId.ToLower());

        if (!string.IsNullOrWhiteSpace(request.CapabilityId))
            query = query.Where(e =>
                e.CapabilityId.ToLower() == request.CapabilityId.ToLower());

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            query = query.Where(e => e.CorrelationId == request.CorrelationId);

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        if (request.FromRequestedAt.HasValue)
            query = query.Where(e => e.RequestedAt >= request.FromRequestedAt.Value);

        if (request.ToRequestedAt.HasValue)
            query = query.Where(e => e.RequestedAt <= request.ToRequestedAt.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(e => e.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new DeviceCommandExecutionDto(
                e.Id,
                e.DeviceId,
                e.CapabilityId,
                e.EndpointId,
                e.CorrelationId,
                e.Operation,
                e.Status,
                e.RequestPayload,
                e.ResultPayload,
                e.Error,
                e.RequestedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<DeviceCommandExecutionDto>(items, page, pageSize, totalCount);
    }
}
