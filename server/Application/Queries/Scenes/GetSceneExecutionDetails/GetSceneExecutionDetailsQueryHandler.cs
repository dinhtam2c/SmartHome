using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Scenes.GetSceneExecutionDetails;

public sealed class GetSceneExecutionDetailsQueryHandler
    : IRequestHandler<GetSceneExecutionDetailsQuery, SceneExecutionDetailsDto>
{
    private readonly IAppReadDbContext _context;

    public GetSceneExecutionDetailsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<SceneExecutionDetailsDto> Handle(
        GetSceneExecutionDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var sceneExists = await _context.Scenes
            .AsNoTracking()
            .AnyAsync(
                scene => scene.Id == request.SceneId && scene.HomeId == request.HomeId,
                cancellationToken);

        if (!sceneExists)
            throw new SceneNotFoundException(request.SceneId);

        var execution = await _context.SceneExecutions
            .AsNoTracking()
            .Include(item => item.Actions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == request.ExecutionId
                    && item.SceneId == request.SceneId
                    && item.HomeId == request.HomeId,
                cancellationToken)
            ?? throw new SceneExecutionNotFoundException(request.ExecutionId);

        var actionDtos = execution.Actions
            .OrderBy(action => action.Section)
            .ThenBy(action => action.Order)
            .Select(ActionSetDtoMapper.ToDto)
            .ToList();

        return new SceneExecutionDetailsDto(
            execution.Id,
            execution.SceneId,
            execution.HomeId,
            execution.Status,
            execution.Phase.ToWireName(),
            execution.FailureBranchSelected,
            execution.TriggerSource,
            execution.StartedAt,
            execution.FinishedAt,
            execution.TotalActions,
            execution.PendingActions,
            execution.SkippedActions,
            execution.SuccessfulActions,
            execution.FailedActions,
            actionDtos);
    }
}
