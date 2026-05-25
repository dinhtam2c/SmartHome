namespace Application.Automations.Evaluation;

public sealed record AutomationEvaluationWorkItem(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    Dictionary<string, object?> State,
    long ReportedAt);
