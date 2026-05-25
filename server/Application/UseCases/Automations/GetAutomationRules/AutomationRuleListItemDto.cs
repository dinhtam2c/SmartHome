using Domain.Models.Automations;

namespace Application.UseCases.Automations.GetAutomationRules;

public sealed record AutomationRuleListItemDto(
    Guid Id,
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    AutomationConditionLogic ConditionLogic,
    int CooldownMs,
    int ConditionCount,
    int MainActionCount,
    int HookActionCount,
    AutomationTimeWindowListItemDto TimeWindow,
    AutomationConditionListItemDto? FirstCondition,
    bool? LastEvaluationResult,
    long? LastEvaluatedAt,
    long? LastTriggeredAt,
    long UpdatedAt
);

public sealed record AutomationConditionListItemDto(
    Guid? DeviceId,
    string? EndpointId,
    string? CapabilityId,
    string? FieldPath,
    AutomationConditionOperator? Operator,
    object? CompareValue
);

public sealed record AutomationTimeWindowListItemDto(
    bool Enabled,
    string? StartTime,
    string? EndTime,
    IReadOnlyList<DayOfWeek> DaysOfWeek
);
