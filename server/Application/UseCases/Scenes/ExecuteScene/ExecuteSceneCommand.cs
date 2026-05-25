using MediatR;

namespace Application.UseCases.Scenes.ExecuteScene;

public sealed record ExecuteSceneCommand(
    Guid HomeId,
    Guid SceneId
) : IRequest<Guid>;
