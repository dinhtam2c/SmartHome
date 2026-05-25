using Application.BusinessServices.Capabilities.Validation;
using Application.Ports.Registries;
using Application.BusinessServices.ActionSets.Validation;
using Application.BusinessServices.Scenes.Realtime;
using Application.Ports.Persistence;
using Application.Common.Realtime;
using Application.Ports.Realtime;
using Application.Common.Errors;
using Domain.Models.Scenes;
using MediatR;

namespace Application.UseCases.Scenes.UpdateScene;

public sealed class UpdateSceneCommandHandler : IRequestHandler<UpdateSceneCommand>
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IAppReadDbContext _context;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly ICapabilityStateValidator _capabilityStateValidator;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSceneCommandHandler(
        ISceneRepository sceneRepository,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityCommandValidator capabilityCommandValidator,
        ICapabilityStateValidator capabilityStateValidator,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _sceneRepository = sceneRepository;
        _context = context;
        _capabilityRegistry = capabilityRegistry;
        _capabilityCommandValidator = capabilityCommandValidator;
        _capabilityStateValidator = capabilityStateValidator;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateSceneCommand request, CancellationToken cancellationToken)
    {
        var scene = await _sceneRepository.GetById(request.SceneId, cancellationToken)
            ?? throw new SceneNotFoundException(request.SceneId);

        if (scene.HomeId != request.HomeId)
            throw new SceneNotFoundException(request.SceneId);

        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Scene name is required.");

        scene.UpdateInfo(request.Name, request.Description, request.IsEnabled);

        if (request.ActionSet is not null)
        {
            var actionSet = await ActionSetValidationHelper.ValidateAndBuildDefinition(
                request.HomeId,
                request.ActionSet,
                _context,
                _capabilityRegistry,
                _capabilityStateValidator,
                _capabilityCommandValidator,
                cancellationToken);

            scene.ReplaceActionSet(actionSet);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtimePublisher.PublishToHome(
            scene.HomeId,
            SceneRealtime.ForScene(scene, RealtimeChanges.Updated),
            cancellationToken);
    }
}
