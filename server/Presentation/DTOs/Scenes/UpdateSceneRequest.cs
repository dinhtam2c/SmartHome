using Presentation.ActionSets;

namespace Presentation.Scenes;

public sealed record UpdateSceneRequest(
    string? Name,
    string? Description,
    bool? IsEnabled,
    ActionSetRequest? ActionSet
);
