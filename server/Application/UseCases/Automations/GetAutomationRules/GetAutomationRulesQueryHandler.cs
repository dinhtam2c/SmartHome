using Application.Ports.Persistence;
using Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Automations.GetAutomationRules;

public sealed class GetAutomationRulesQueryHandler
    : IRequestHandler<GetAutomationRulesQuery, IReadOnlyList<AutomationRuleListItemDto>>
{
    private readonly IAppReadDbContext _context;

    public GetAutomationRulesQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AutomationRuleListItemDto>> Handle(
        GetAutomationRulesQuery request,
        CancellationToken cancellationToken)
    {
        var homeExists = await _context.Homes
            .AsNoTracking()
            .AnyAsync(home => home.Id == request.HomeId, cancellationToken);

        if (!homeExists)
            throw new HomeNotFoundException(request.HomeId);

        var rules = await _context.AutomationRules
            .AsNoTracking()
            .Include(rule => rule.Conditions)
            .Include(rule => rule.ActionSet)
            .ThenInclude(actionSet => actionSet.Actions)
            .AsSplitQuery()
            .Where(rule => rule.HomeId == request.HomeId)
            .OrderBy(rule => rule.Name)
            .ToListAsync(cancellationToken);

        return rules
            .Select(rule => new AutomationRuleListItemDto(
                rule.Id,
                rule.HomeId,
                rule.Name,
                rule.Description,
                rule.IsEnabled,
                rule.ConditionLogic,
                rule.CooldownMs,
                rule.Conditions.Count,
                rule.ActionSet.Actions.Count(action => action.Section == Domain.Models.ActionSets.ActionSetSection.Main),
                rule.ActionSet.Actions.Count(action => action.Section != Domain.Models.ActionSets.ActionSetSection.Main),
                ToTimeWindowDto(rule),
                rule.Conditions
                    .OrderBy(condition => condition.Order)
                    .Select(condition => new AutomationConditionListItemDto(
                        condition.DeviceId,
                        condition.EndpointId,
                        condition.CapabilityId,
                        condition.FieldPath,
                        condition.Operator,
                        condition.CompareValue))
                    .FirstOrDefault(),
                rule.LastEvaluationResult,
                rule.LastEvaluatedAt,
                rule.LastTriggeredAt,
                rule.UpdatedAt))
            .ToList();
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

    private static AutomationTimeWindowListItemDto ToTimeWindowDto(Domain.Models.Automations.AutomationRule rule)
    {
        return !rule.TimeWindowEnabled
            ? new AutomationTimeWindowListItemDto(false, null, null, [])
            : new AutomationTimeWindowListItemDto(
                true,
                FormatMinute(rule.TimeWindowStartMinute),
                FormatMinute(rule.TimeWindowEndMinute),
                DaysFromMask(rule.TimeWindowDaysOfWeekMask));
    }
}
