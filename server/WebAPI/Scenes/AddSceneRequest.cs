namespace WebAPI.Scenes;

public sealed record AddSceneRequest(
    string Name,
    string? Description,
    bool IsEnabled,
    IEnumerable<SceneTargetRequest>? Targets,
    IEnumerable<SceneSideEffectRequest>? SideEffects
);
