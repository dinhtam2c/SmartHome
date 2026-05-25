namespace WebAPI.Automations;

public sealed record ExecuteAutomationRuleRequest(
    string? TriggerSource,
    Guid? TriggerDeviceId,
    string? TriggerEndpointId,
    string? TriggerCapabilityId,
    Dictionary<string, object?>? TriggerState
);
