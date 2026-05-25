using MediatR;

namespace Application.Queries.Scenes.GetScenes;

public sealed record GetScenesQuery(Guid HomeId) : IRequest<IReadOnlyList<SceneListItemDto>>;
