using Application.Common.Data;
using Application.Exceptions;
using Application.Services;
using Core.Domain.Scenes;
using MediatR;

namespace Application.Commands.Scenes.UpdateScene;

public sealed class UpdateSceneCommandHandler : IRequestHandler<UpdateSceneCommand>
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IAppReadDbContext _context;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly ICapabilityStateValidator _capabilityStateValidator;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSceneCommandHandler(
        ISceneRepository sceneRepository,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityCommandValidator capabilityCommandValidator,
        ICapabilityStateValidator capabilityStateValidator,
        IUnitOfWork unitOfWork)
    {
        _sceneRepository = sceneRepository;
        _context = context;
        _capabilityRegistry = capabilityRegistry;
        _capabilityCommandValidator = capabilityCommandValidator;
        _capabilityStateValidator = capabilityStateValidator;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateSceneCommand request, CancellationToken cancellationToken)
    {
        var scene = await _sceneRepository.GetById(request.SceneId, cancellationToken)
            ?? throw new SceneNotFoundException(request.SceneId);

        if (scene.HomeId != request.HomeId)
            throw new SceneNotFoundException(request.SceneId);

        scene.UpdateInfo(request.Name, request.Description, request.IsEnabled);

        List<SceneTargetDefinition>? targetDefinitions = null;

        if (request.Targets is not null)
        {
            targetDefinitions = await SceneTargetValidationHelper.ValidateAndBuildDefinitions(
                request.HomeId,
                request.Targets,
                _context,
                _capabilityRegistry,
                _capabilityStateValidator,
                cancellationToken);

            scene.ReplaceTargets(targetDefinitions);
        }

        if (request.SideEffects is not null)
        {
            targetDefinitions ??= scene.Targets
                .OrderBy(target => target.Order)
                .Select(target => new SceneTargetDefinition(
                    target.DeviceId,
                    target.EndpointId,
                    target.CapabilityId,
                    target.GetDesiredState()))
                .ToList();

            var sideEffectDefinitions = await SceneTargetValidationHelper.ValidateAndBuildSideEffectDefinitions(
                request.HomeId,
                targetDefinitions,
                request.SideEffects,
                _context,
                _capabilityRegistry,
                _capabilityCommandValidator,
                cancellationToken);

            scene.ReplaceSideEffects(sideEffectDefinitions);
        }

        if (scene.Targets.Count == 0 && scene.SideEffects.Count == 0)
            throw new DomainValidationException("Scene must contain at least one target or side effect.");

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
