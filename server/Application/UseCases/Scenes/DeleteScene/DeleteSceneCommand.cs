using MediatR;

namespace Application.UseCases.Scenes.DeleteScene;

public sealed record DeleteSceneCommand(
    Guid HomeId,
    Guid SceneId
) : IRequest;
