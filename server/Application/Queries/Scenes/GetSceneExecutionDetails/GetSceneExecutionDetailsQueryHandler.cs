using Application.Common.Data;
using Application.Exceptions;
using Application.Queries.Scenes;
using Core.Domain.Scenes;
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
            .Include(item => item.Targets)
            .Include(item => item.SideEffects)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == request.ExecutionId
                    && item.SceneId == request.SceneId
                    && item.HomeId == request.HomeId,
                cancellationToken)
            ?? throw new SceneExecutionNotFoundException(request.ExecutionId);

        var targetDtos = execution.Targets
            .OrderBy(target => target.Order)
            .Select(target => new SceneExecutionTargetDetailsDto(
                target.Id,
                target.SceneTargetId,
                target.DeviceId,
                target.EndpointId,
                target.CapabilityId,
                ScenePayloadMapper.ParseDesiredState(target.DesiredStatePayload),
                target.Status,
                target.CommandCorrelationId,
                target.GetUnresolvedDiff(),
                target.Error,
                target.Order,
                target.UpdatedAt))
            .ToList();

        var sideEffectDtos = execution.SideEffects
            .OrderBy(sideEffect => sideEffect.Order)
            .Select(sideEffect => new SceneExecutionSideEffectDetailsDto(
                sideEffect.Id,
                sideEffect.SceneSideEffectId,
                sideEffect.DeviceId,
                sideEffect.EndpointId,
                sideEffect.CapabilityId,
                sideEffect.Operation,
                ScenePayloadMapper.ParseDesiredState(sideEffect.ParamsPayload),
                sideEffect.Timing,
                sideEffect.DelayMs,
                sideEffect.Status,
                sideEffect.CommandCorrelationId,
                sideEffect.Error,
                sideEffect.Order,
                sideEffect.UpdatedAt))
            .ToList();

        return new SceneExecutionDetailsDto(
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
            execution.FailedSideEffects,
            targetDtos,
            sideEffectDtos);
    }
}
