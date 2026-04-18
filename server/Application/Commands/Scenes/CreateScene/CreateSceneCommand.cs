using MediatR;

namespace Application.Commands.Scenes.CreateScene;

public sealed record CreateSceneCommand(
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    IEnumerable<SceneTargetModel>? Targets,
    IEnumerable<SceneSideEffectModel>? SideEffects
) : IRequest<Guid>;
