using Application.Queries.Devices.GetDeviceCommandExecutions;
using Core.Domain.Scenes;
using MediatR;

namespace Application.Queries.Scenes.GetSceneExecutions;

public sealed record GetSceneExecutionsQuery(
    Guid HomeId,
    Guid SceneId,
    SceneExecutionStatus? Status,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<SceneExecutionListItemDto>>;
