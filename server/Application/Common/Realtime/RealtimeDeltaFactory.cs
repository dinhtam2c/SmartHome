using Core.Domain.Automations;
using Core.Domain.ActionSets;
using Core.Domain.DeviceCommands;
using Core.Domain.Scenes;

namespace Application.Common.Realtime;

public static class RealtimeDeltaFactory
{
    public static RealtimeDelta ForScene(Scene scene, string change)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.Scene,
            change: change,
            homeId: scene.HomeId,
            sceneId: scene.Id,
            delta: new
            {
                scene.Id,
                scene.HomeId,
                scene.Name,
                scene.Description,
                scene.IsEnabled,
                MainActionCount = scene.Actions.Count(action => action.Section == ActionSetSection.Main),
                HookActionCount = scene.Actions.Count(action => action.Section != ActionSetSection.Main),
                scene.UpdatedAt
            });
    }

    public static RealtimeDelta ForSceneDeleted(Guid homeId, Guid sceneId)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.Scene,
            change: RealtimeChanges.Deleted,
            homeId: homeId,
            sceneId: sceneId);
    }

    public static RealtimeDelta ForSceneExecution(SceneExecution execution)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.SceneExecution,
            change: RealtimeChanges.Updated,
            homeId: execution.HomeId,
            sceneId: execution.SceneId,
            executionId: execution.Id,
            delta: new
            {
                execution.Id,
                execution.SceneId,
                execution.HomeId,
                execution.Status,
                Phase = execution.Phase.ToWireName(),
                execution.StartedAt,
                execution.FinishedAt,
                execution.TotalActions,
                execution.PendingActions,
                execution.SkippedActions,
                execution.SuccessfulActions,
                execution.FailedActions
            });
    }

    public static RealtimeDelta ForAutomationRule(AutomationRule rule, string change)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.AutomationRule,
            change: change,
            homeId: rule.HomeId,
            ruleId: rule.Id,
            delta: ToAutomationRuleSummary(rule));
    }

    public static RealtimeDelta ForAutomationRuleDeleted(Guid homeId, Guid ruleId)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.AutomationRule,
            change: RealtimeChanges.Deleted,
            homeId: homeId,
            ruleId: ruleId);
    }

    public static RealtimeDelta ForAutomationExecution(AutomationExecution execution)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.AutomationExecution,
            change: RealtimeChanges.Updated,
            homeId: execution.HomeId,
            ruleId: execution.RuleId,
            executionId: execution.Id,
            delta: new
            {
                execution.Id,
                execution.RuleId,
                execution.HomeId,
                execution.Status,
                Phase = execution.Phase.ToWireName(),
                execution.StartedAt,
                execution.FinishedAt,
                execution.TotalActions,
                execution.PendingActions,
                execution.SkippedActions,
                execution.SuccessfulActions,
                execution.FailedActions,
                execution.TriggerDeviceId,
                execution.TriggerEndpointId,
                execution.TriggerCapabilityId,
                execution.TriggerSource
            });
    }

    public static RealtimeDelta ForDeviceCommandExecution(DeviceCommandExecution execution)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.DeviceCommandExecution,
            change: RealtimeChanges.Updated,
            deviceId: execution.DeviceId,
            endpointId: execution.EndpointId,
            capabilityId: execution.CapabilityId,
            correlationId: execution.CorrelationId,
            delta: new
            {
                execution.Id,
                execution.DeviceId,
                execution.EndpointId,
                execution.CapabilityId,
                execution.CorrelationId,
                execution.Operation,
                execution.Status,
                execution.RequestPayload,
                execution.ResultPayload,
                execution.Error,
                execution.RequestedAt
            });
    }

    public static RealtimeDelta ForDeviceCommandExecution(Guid deviceId)
    {
        return RealtimeDelta.Create(
            entity: RealtimeEntities.DeviceCommandExecution,
            change: RealtimeChanges.Updated,
            deviceId: deviceId);
    }

    private static object ToAutomationRuleSummary(AutomationRule rule)
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
            MainActionCount = rule.Actions.Count(action => action.Section == ActionSetSection.Main),
            HookActionCount = rule.Actions.Count(action => action.Section != ActionSetSection.Main),
            TimeWindow = ToTimeWindowSummary(rule),
            FirstCondition = rule.Conditions
                .OrderBy(condition => condition.Order)
                .Select(ToConditionSummary)
                .FirstOrDefault(),
            rule.LastEvaluationResult,
            rule.LastEvaluatedAt,
            rule.LastTriggeredAt,
            rule.UpdatedAt
        };
    }

    private static object? ToConditionSummary(AutomationCondition condition)
    {
        return new
        {
            condition.DeviceId,
            condition.EndpointId,
            condition.CapabilityId,
            condition.FieldPath,
            condition.Operator,
            CompareValue = condition.GetCompareValue()
        };
    }

    private static object ToTimeWindowSummary(AutomationRule rule)
    {
        return new
        {
            Enabled = rule.TimeWindowEnabled,
            StartTime = FormatMinute(rule.TimeWindowStartMinute),
            EndTime = FormatMinute(rule.TimeWindowEndMinute),
            DaysOfWeek = DaysFromMask(rule.TimeWindowDaysOfWeekMask)
        };
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
        return Enum.GetValues<DayOfWeek>()
            .Where(day => (mask & (1 << (int)day)) != 0)
            .ToList();
    }
}
