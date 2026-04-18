using Core.Domain.Scenes;

namespace Application.Queries.Scenes.GetSceneExecutions;

public sealed record SceneExecutionListItemDto(
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
    int FailedSideEffects
);
