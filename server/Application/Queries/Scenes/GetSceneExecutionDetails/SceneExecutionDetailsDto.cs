using Core.Domain.Scenes;

namespace Application.Queries.Scenes.GetSceneExecutionDetails;

public sealed record SceneExecutionDetailsDto(
    Guid Id,
    Guid SceneId,
    Guid HomeId,
    SceneExecutionStatus Status,
    string Phase,
    bool FailureBranchSelected,
    string? TriggerSource,
    long StartedAt,
    long? FinishedAt,
    int TotalActions,
    int PendingActions,
    int SkippedActions,
    int SuccessfulActions,
    int FailedActions,
    IReadOnlyList<ActionExecutionDto> Actions
);
