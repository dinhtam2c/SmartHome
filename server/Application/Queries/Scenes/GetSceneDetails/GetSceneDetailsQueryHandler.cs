using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Scenes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Scenes.GetSceneDetails;

public sealed class GetSceneDetailsQueryHandler : IRequestHandler<GetSceneDetailsQuery, SceneDetailsDto>
{
    private readonly IAppReadDbContext _context;

    public GetSceneDetailsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<SceneDetailsDto> Handle(
        GetSceneDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var scene = await _context.Scenes
            .AsNoTracking()
            .Include(item => item.Targets)
            .Include(item => item.SideEffects)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                item => item.Id == request.SceneId && item.HomeId == request.HomeId,
                cancellationToken)
            ?? throw new SceneNotFoundException(request.SceneId);

        var targetDtos = scene.Targets
            .OrderBy(target => target.Order)
            .Select(target => new SceneTargetDetailsDto(
                target.Id,
                target.DeviceId,
                target.EndpointId,
                target.CapabilityId,
                ScenePayloadMapper.ParseDesiredState(target.DesiredStatePayload),
                target.Order))
            .ToList();

        var sideEffectDtos = scene.SideEffects
            .OrderBy(sideEffect => sideEffect.Order)
            .Select(sideEffect => new SceneSideEffectDetailsDto(
                sideEffect.Id,
                sideEffect.DeviceId,
                sideEffect.EndpointId,
                sideEffect.CapabilityId,
                sideEffect.Operation,
                ScenePayloadMapper.ParseDesiredState(sideEffect.ParamsPayload),
                sideEffect.Timing,
                sideEffect.DelayMs,
                sideEffect.Order))
            .ToList();

        return new SceneDetailsDto(
            scene.Id,
            scene.HomeId,
            scene.Name,
            scene.Description,
            scene.IsEnabled,
            scene.CreatedAt,
            scene.UpdatedAt,
            targetDtos,
            sideEffectDtos);
    }
}
