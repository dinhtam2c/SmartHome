using MediatR;

namespace Application.Commands.Scenes.ExecuteScene;

public sealed record ExecuteSceneCommand(
    Guid HomeId,
    Guid SceneId,
    string? TriggerSource
) : IRequest<Guid>;
