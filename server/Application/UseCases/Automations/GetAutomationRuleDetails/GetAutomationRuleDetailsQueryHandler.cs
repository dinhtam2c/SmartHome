using Application.BusinessServices.ActionSets.Contracts;
using Application.Ports.Persistence;
using Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Automations.GetAutomationRuleDetails;

public sealed class GetAutomationRuleDetailsQueryHandler
    : IRequestHandler<GetAutomationRuleDetailsQuery, AutomationRuleDetailsDto>
{
    private readonly IAppReadDbContext _context;

    public GetAutomationRuleDetailsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<AutomationRuleDetailsDto> Handle(
        GetAutomationRuleDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _context.AutomationRules
            .AsNoTracking()
            .Include(item => item.Conditions)
            .Include(item => item.ActionSet)
            .ThenInclude(actionSet => actionSet.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                item => item.Id == request.RuleId && item.HomeId == request.HomeId,
                cancellationToken)
            ?? throw new AutomationRuleNotFoundException(request.RuleId);

        return new AutomationRuleDetailsDto(
            rule.Id,
            rule.HomeId,
            rule.Name,
            rule.Description,
            rule.IsEnabled,
            rule.ConditionLogic,
            rule.CooldownMs,
            rule.LastEvaluationResult,
            rule.LastEvaluatedAt,
            rule.LastTriggeredAt,
            rule.CreatedAt,
            rule.UpdatedAt,
            ToTimeWindowDto(rule),
            rule.Conditions
                .OrderBy(condition => condition.Order)
                .Select(condition => new AutomationConditionDetailsDto(
                    condition.Id,
                    condition.DeviceId,
                    condition.EndpointId,
                    condition.CapabilityId,
                    condition.FieldPath,
                    condition.Operator,
                    condition.CompareValue,
                    condition.Order))
                .ToList(),
            ActionSetViewMapper.ForAutomationRule(rule));
    }

    private static string? FormatMinute(int? minute)
    {
        if (!minute.HasValue)
            return null;

        var hour = minute.Value / 60;
        var minutes = minute.Value % 60;
        return $"{hour:00}:{minutes:00}";
    }

    private static IReadOnlyList<DayOfWeek> DaysFromMask(int mask)
    {
        var normalizedMask = mask == 0 ? 0b111_1111 : mask;
        return Enum.GetValues<DayOfWeek>()
            .Where(day => (normalizedMask & (1 << (int)day)) != 0)
            .ToList();
    }

    private static AutomationTimeWindowDto ToTimeWindowDto(Domain.Models.Automations.AutomationRule rule)
    {
        return !rule.TimeWindowEnabled
            ? new AutomationTimeWindowDto(false, null, null, [])
            : new AutomationTimeWindowDto(
                true,
                FormatMinute(rule.TimeWindowStartMinute),
                FormatMinute(rule.TimeWindowEndMinute),
                DaysFromMask(rule.TimeWindowDaysOfWeekMask));
    }
}
