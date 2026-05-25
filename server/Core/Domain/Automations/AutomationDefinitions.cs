namespace Core.Domain.Automations;

public sealed record AutomationConditionDefinition(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    string FieldPath,
    AutomationConditionOperator Operator,
    object? CompareValue)
{
    public static AutomationConditionDefinition DeviceState(
        Guid deviceId,
        string endpointId,
        string capabilityId,
        string fieldPath,
        AutomationConditionOperator op,
        object? compareValue)
    {
        return new AutomationConditionDefinition(
            deviceId,
            endpointId,
            capabilityId,
            fieldPath,
            op,
            compareValue);
    }
}

public sealed record AutomationTimeWindowDefinition(
    int StartMinute,
    int EndMinute,
    int DaysOfWeekMask);

public sealed record AutomationTriggerContext(
    Guid? TriggerDeviceId,
    string? TriggerEndpointId,
    string? TriggerCapabilityId,
    Dictionary<string, object?>? TriggerState,
    string? TriggerSource
);

public enum AutomationConditionLogic
{
    All,
    Any
}

public enum AutomationConditionOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between
}
