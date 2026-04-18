using Application.Common.Data;
using Application.Exceptions;
using Application.Queries.Devices.GetDeviceCommandExecutions;
using Core.Domain.Scenes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Scenes.GetSceneExecutions;

public sealed class GetSceneExecutionsQueryHandler
    : IRequestHandler<GetSceneExecutionsQuery, PagedResult<SceneExecutionListItemDto>>
{
    private readonly IAppReadDbContext _context;

    public GetSceneExecutionsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<SceneExecutionListItemDto>> Handle(
        GetSceneExecutionsQuery request,
        CancellationToken cancellationToken)
    {
        var sceneExists = await _context.Scenes
            .AsNoTracking()
            .AnyAsync(
                scene => scene.Id == request.SceneId && scene.HomeId == request.HomeId,
                cancellationToken);

        if (!sceneExists)
            throw new SceneNotFoundException(request.SceneId);

        var query = _context.SceneExecutions
            .AsNoTracking()
            .Where(execution =>
                execution.SceneId == request.SceneId
                && execution.HomeId == request.HomeId);

        if (request.Status.HasValue)
            query = query.Where(execution => execution.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(execution => execution.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(execution => new SceneExecutionListItemDto(
                execution.Id,
                execution.SceneId,
                execution.HomeId,
                execution.Status,
                execution.TriggerSource,
                execution.StartedAt,
                execution.FinishedAt,
                execution.TotalTargets,
                execution.PendingTargets,
                execution.SkippedTargets,
                execution.SuccessfulTargets,
                  execution.FailedTargets,
                  execution.SideEffects.Count(sideEffect =>
                      sideEffect.Status == SceneExecutionSideEffectStatus.Failed)))
            .ToListAsync(cancellationToken);

        return new PagedResult<SceneExecutionListItemDto>(items, page, pageSize, totalCount);
    }
}
