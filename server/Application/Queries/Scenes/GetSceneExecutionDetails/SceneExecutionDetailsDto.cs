using Core.Domain.Scenes;

namespace Application.Queries.Scenes.GetSceneExecutionDetails;

public sealed record SceneExecutionDetailsDto(
    Guid Id,
    Guid SceneId,
    Guid HomeId,
    SceneExecutionStatus Status,
    string? TriggerSource,
    long StartedAt,
    long? FinishedAt,
    int TotalTargets,
    int PendingTargets,
    int SkippedTargets,
    int SuccessfulTargets,
    int FailedTargets,
    int FailedSideEffects,
    IReadOnlyList<SceneExecutionTargetDetailsDto> Targets,
    IReadOnlyList<SceneExecutionSideEffectDetailsDto> SideEffects
);

public sealed record SceneExecutionTargetDetailsDto(
    Guid Id,
    Guid SceneTargetId,
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    Dictionary<string, object?> DesiredState,
    SceneExecutionTargetStatus Status,
    string? CommandCorrelationId,
    Dictionary<string, object?>? UnresolvedDiff,
    string? Error,
    int Order,
    long UpdatedAt
);

public sealed record SceneExecutionSideEffectDetailsDto(
    Guid Id,
    Guid SceneSideEffectId,
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    string Operation,
    Dictionary<string, object?> Params,
    SceneSideEffectTiming Timing,
    int DelayMs,
    SceneExecutionSideEffectStatus Status,
    string? CommandCorrelationId,
    string? Error,
    int Order,
    long UpdatedAt
);
