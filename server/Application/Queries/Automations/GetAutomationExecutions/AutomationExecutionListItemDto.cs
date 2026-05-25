using Core.Domain.Automations;

namespace Application.Queries.Automations.GetAutomationExecutions;

public sealed record AutomationExecutionListItemDto(
    Guid Id,
    Guid RuleId,
    Guid HomeId,
    AutomationExecutionStatus Status,
    string Phase,
    string? TriggerSource,
    Guid? TriggerDeviceId,
    string? TriggerEndpointId,
    string? TriggerCapabilityId,
    long StartedAt,
    long? FinishedAt,
    int TotalActions,
    int PendingActions,
    int SkippedActions,
    int SuccessfulActions,
    int FailedActions
);
