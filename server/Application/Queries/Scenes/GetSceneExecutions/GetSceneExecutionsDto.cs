using Core.Domain.Scenes;

namespace Application.Queries.Scenes.GetSceneExecutions;

public sealed record SceneExecutionListItemDto(
    Guid Id,
    Guid SceneId,
    Guid HomeId,
    SceneExecutionStatus Status,
    string Phase,
    string? TriggerSource,
    long StartedAt,
    long? FinishedAt,
    int TotalActions,
    int PendingActions,
    int SkippedActions,
    int SuccessfulActions,
    int FailedActions
);
