namespace Application.Queries.Scenes.GetScenes;

public sealed record SceneListItemDto(
    Guid Id,
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    int TargetCount,
    long UpdatedAt
);
