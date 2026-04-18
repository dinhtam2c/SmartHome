using MediatR;

namespace Application.Commands.Scenes.UpdateScene;

public sealed record UpdateSceneCommand(
    Guid HomeId,
    Guid SceneId,
    string? Name,
    string? Description,
    bool? IsEnabled,
    IEnumerable<SceneTargetModel>? Targets,
    IEnumerable<SceneSideEffectModel>? SideEffects
) : IRequest;
