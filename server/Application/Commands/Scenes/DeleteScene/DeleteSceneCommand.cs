using MediatR;

namespace Application.Commands.Scenes.DeleteScene;

public sealed record DeleteSceneCommand(
    Guid HomeId,
    Guid SceneId
) : IRequest;
