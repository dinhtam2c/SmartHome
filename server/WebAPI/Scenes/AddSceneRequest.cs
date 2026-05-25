using WebAPI.ActionSets;

namespace WebAPI.Scenes;

public sealed record AddSceneRequest(
    string Name,
    string? Description,
    bool IsEnabled,
    ActionSetRequest? ActionSet
);
