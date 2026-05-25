using Domain.Models.Automations;

namespace Application.BusinessServices.Automations.Rules;

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
