namespace WebAPI.Scenes;

public sealed record UpdateSceneRequest(
    string? Name,
    string? Description,
    bool? IsEnabled,
    IEnumerable<SceneTargetRequest>? Targets,
    IEnumerable<SceneSideEffectRequest>? SideEffects
);
