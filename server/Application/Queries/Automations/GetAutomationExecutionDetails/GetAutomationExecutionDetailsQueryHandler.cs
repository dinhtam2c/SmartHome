using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Automations.GetAutomationExecutionDetails;

public sealed class GetAutomationExecutionDetailsQueryHandler
    : IRequestHandler<GetAutomationExecutionDetailsQuery, AutomationExecutionDetailsDto>
{
    private readonly IAppReadDbContext _context;

    public GetAutomationExecutionDetailsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<AutomationExecutionDetailsDto> Handle(
        GetAutomationExecutionDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var ruleExists = await _context.AutomationRules
            .AsNoTracking()
            .AnyAsync(
                rule => rule.Id == request.RuleId && rule.HomeId == request.HomeId,
                cancellationToken);

        if (!ruleExists)
            throw new AutomationRuleNotFoundException(request.RuleId);

        var execution = await _context.AutomationExecutions
            .AsNoTracking()
            .Include(item => item.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == request.ExecutionId
                    && item.RuleId == request.RuleId
                    && item.HomeId == request.HomeId,
                cancellationToken)
            ?? throw new AutomationExecutionNotFoundException(request.ExecutionId);

        var actions = execution.Actions
            .OrderBy(action => action.Section)
            .ThenBy(action => action.Order)
            .Select(ActionSetDtoMapper.ToDto)
            .ToList();

        return new AutomationExecutionDetailsDto(
            execution.Id,
            execution.RuleId,
            execution.HomeId,
            execution.Status,
            execution.Phase.ToWireName(),
            execution.FailureBranchSelected,
            execution.TriggerSource,
            execution.TriggerDeviceId,
            execution.TriggerEndpointId,
            execution.TriggerCapabilityId,
            execution.GetTriggerState(),
            execution.StartedAt,
            execution.FinishedAt,
            execution.TotalActions,
            execution.PendingActions,
            execution.SkippedActions,
            execution.SuccessfulActions,
            execution.FailedActions,
            actions);
    }
}
