using Core.Domain.Automations;

namespace Application.Automations.Rules;

public sealed record AutomationConditionModel(
    Guid? DeviceId,
    string? EndpointId,
    string? CapabilityId,
    string? FieldPath,
    AutomationConditionOperator? Operator,
    object? CompareValue
);

public sealed record AutomationTimeWindowModel(
    bool Enabled,
    string? StartTime,
    string? EndTime,
    IReadOnlyList<DayOfWeek>? DaysOfWeek
);
