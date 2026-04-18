using MediatR;

namespace Application.Queries.Scenes.GetSceneExecutionDetails;

public sealed record GetSceneExecutionDetailsQuery(
    Guid HomeId,
    Guid SceneId,
    Guid ExecutionId
) : IRequest<SceneExecutionDetailsDto>;
