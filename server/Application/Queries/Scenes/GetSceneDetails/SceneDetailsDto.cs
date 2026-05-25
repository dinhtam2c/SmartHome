namespace Application.Queries.Scenes.GetSceneDetails;

public sealed record SceneDetailsDto(
    Guid Id,
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    long CreatedAt,
    long UpdatedAt,
    ActionSetDto ActionSet
);
