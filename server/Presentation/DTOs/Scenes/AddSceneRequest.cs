using Presentation.ActionSets;

namespace Presentation.Scenes;

public sealed record AddSceneRequest(
    string Name,
    string? Description,
    bool IsEnabled,
    ActionSetRequest? ActionSet
);
