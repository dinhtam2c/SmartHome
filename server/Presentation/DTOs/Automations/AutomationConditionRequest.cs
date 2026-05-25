using Domain.Models.Automations;

namespace Presentation.Automations;

public sealed record AutomationConditionRequest(
    Guid? DeviceId,
    string? EndpointId,
    string? CapabilityId,
    string? FieldPath,
    AutomationConditionOperator? Operator,
    object? CompareValue
);

public sealed record AutomationTimeWindowRequest(
    bool Enabled,
    string? StartTime,
    string? EndTime,
    IReadOnlyList<DayOfWeek>? DaysOfWeek
);
