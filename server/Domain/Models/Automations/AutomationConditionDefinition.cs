namespace Domain.Models.Automations;

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
