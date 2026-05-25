using Core.Domain.Automations;

namespace Application.Queries.Automations.GetAutomationExecutionDetails;

public sealed record AutomationExecutionDetailsDto(
    Guid Id,
    Guid RuleId,
    Guid HomeId,
    AutomationExecutionStatus Status,
    string Phase,
    bool FailureBranchSelected,
    string? TriggerSource,
    Guid? TriggerDeviceId,
    string? TriggerEndpointId,
    string? TriggerCapabilityId,
    Dictionary<string, object?>? TriggerState,
    long StartedAt,
    long? FinishedAt,
    int TotalActions,
    int PendingActions,
    int SkippedActions,
    int SuccessfulActions,
    int FailedActions,
    IReadOnlyList<ActionExecutionDto> Actions
);
