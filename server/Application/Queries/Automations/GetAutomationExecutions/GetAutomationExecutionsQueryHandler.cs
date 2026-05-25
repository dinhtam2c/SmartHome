using Application.Common.Data;
using Application.Exceptions;
using Application.Queries.Devices.GetDeviceCommandExecutions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Automations.GetAutomationExecutions;

public sealed class GetAutomationExecutionsQueryHandler
    : IRequestHandler<GetAutomationExecutionsQuery, PagedResult<AutomationExecutionListItemDto>>
{
    private readonly IAppReadDbContext _context;

    public GetAutomationExecutionsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AutomationExecutionListItemDto>> Handle(
        GetAutomationExecutionsQuery request,
        CancellationToken cancellationToken)
    {
        var ruleExists = await _context.AutomationRules
            .AsNoTracking()
            .AnyAsync(
                rule => rule.Id == request.RuleId && rule.HomeId == request.HomeId,
                cancellationToken);

        if (!ruleExists)
            throw new AutomationRuleNotFoundException(request.RuleId);

        var query = _context.AutomationExecutions
            .AsNoTracking()
            .Where(execution =>
                execution.RuleId == request.RuleId
                && execution.HomeId == request.HomeId);

        if (request.Status.HasValue)
            query = query.Where(execution => execution.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(execution => execution.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(execution => new AutomationExecutionListItemDto(
                execution.Id,
                execution.RuleId,
                execution.HomeId,
                execution.Status,
                execution.Phase.ToWireName(),
                execution.TriggerSource,
                execution.TriggerDeviceId,
                execution.TriggerEndpointId,
                execution.TriggerCapabilityId,
                execution.StartedAt,
                execution.FinishedAt,
                execution.TotalActions,
                execution.PendingActions,
                execution.SkippedActions,
                execution.SuccessfulActions,
                execution.FailedActions))
            .ToListAsync(cancellationToken);

        return new PagedResult<AutomationExecutionListItemDto>(items, page, pageSize, totalCount);
    }
}
