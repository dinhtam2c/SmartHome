using Application.BusinessServices.ActionSets.Contracts;
using Domain.Models.Automations;

namespace Application.UseCases.Automations.GetAutomationRuleDetails;

public sealed record AutomationRuleDetailsDto(
    Guid Id,
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    AutomationConditionLogic ConditionLogic,
    int CooldownMs,
    bool? LastEvaluationResult,
    long? LastEvaluatedAt,
    long? LastTriggeredAt,
    long CreatedAt,
    long UpdatedAt,
    AutomationTimeWindowDto TimeWindow,
    IReadOnlyList<AutomationConditionDetailsDto> Conditions,
    ActionSetView ActionSet
);

public sealed record AutomationConditionDetailsDto(
    Guid Id,
    Guid? DeviceId,
    string? EndpointId,
    string? CapabilityId,
    string? FieldPath,
    AutomationConditionOperator? Operator,
    object? CompareValue,
    int Order
);

public sealed record AutomationTimeWindowDto(
    bool Enabled,
    string? StartTime,
    string? EndTime,
    IReadOnlyList<DayOfWeek> DaysOfWeek
);
