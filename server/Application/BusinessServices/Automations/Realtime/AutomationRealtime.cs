using Application.BusinessServices.ActionSets.Contracts;
using Application.Common.Realtime;
using Domain.Models.ActionSets;
using Domain.Models.Automations;

namespace Application.BusinessServices.Automations.Realtime;

public static class AutomationRealtime
{
    public static RealtimeDelta ForRule(AutomationRule rule, string change)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.AutomationRule,
            change: change,
            homeId: rule.HomeId,
            ruleId: rule.Id,
            delta: ToRuleSummary(rule));
    }

    public static RealtimeDelta ForDeleted(Guid homeId, Guid ruleId)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.AutomationRule,
            change: RealtimeChanges.Deleted,
            homeId: homeId,
            ruleId: ruleId);
    }

    public static RealtimeDelta ForExecution(ActionSetExecution execution)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.AutomationExecution,
            change: RealtimeChanges.Updated,
            homeId: execution.HomeId,
            ruleId: execution.SourceId,
            executionId: execution.Id,
            delta: new
            {
                execution.Id,
                RuleId = execution.SourceId,
                execution.HomeId,
                execution.Status,
                Phase = execution.Phase.ToWireName(),
                execution.StartedAt,
                execution.FinishedAt
            });
    }

    private static object ToRuleSummary(AutomationRule rule)
    {
        return new
        {
            rule.Id,
            rule.HomeId,
            rule.Name,
            rule.Description,
            rule.IsEnabled,
            rule.ConditionLogic,
            rule.CooldownMs,
            ConditionCount = rule.Conditions.Count,
            MainActionCount = rule.ActionSet.Actions.Count(action => action.Section == ActionSetSection.Main),
            HookActionCount = rule.ActionSet.Actions.Count(action => action.Section != ActionSetSection.Main),
            TimeWindow = new
            {
                Enabled = rule.TimeWindowEnabled,
                StartTime = FormatMinute(rule.TimeWindowStartMinute),
                EndTime = FormatMinute(rule.TimeWindowEndMinute),
                DaysOfWeek = Enum.GetValues<DayOfWeek>()
                    .Where(day => (rule.TimeWindowDaysOfWeekMask & (1 << (int)day)) != 0)
                    .ToList()
            },
            FirstCondition = rule.Conditions
                .OrderBy(condition => condition.Order)
                .Select(condition => new
                {
                    condition.DeviceId,
                    condition.EndpointId,
                    condition.CapabilityId,
                    condition.FieldPath,
                    condition.Operator,
                    condition.CompareValue
                })
                .FirstOrDefault(),
            rule.LastEvaluationResult,
            rule.LastEvaluatedAt,
            rule.LastTriggeredAt,
            rule.UpdatedAt
        };
    }

    private static string? FormatMinute(int? minute)
    {
        return minute.HasValue
            ? $"{minute.Value / 60:00}:{minute.Value % 60:00}"
            : null;
    }
}
