using MediatR;

namespace Application.Queries.Scenes.GetSceneDetails;

public sealed record GetSceneDetailsQuery(
    Guid HomeId,
    Guid SceneId
) : IRequest<SceneDetailsDto>;
