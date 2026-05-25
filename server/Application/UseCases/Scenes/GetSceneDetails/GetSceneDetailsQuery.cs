using MediatR;

namespace Application.UseCases.Scenes.GetSceneDetails;

public sealed record GetSceneDetailsQuery(
    Guid HomeId,
    Guid SceneId
) : IRequest<SceneDetailsDto>;
