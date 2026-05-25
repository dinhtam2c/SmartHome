using WebAPI.ActionSets;

namespace WebAPI.Scenes;

public sealed record UpdateSceneRequest(
    string? Name,
    string? Description,
    bool? IsEnabled,
    ActionSetRequest? ActionSet
);
