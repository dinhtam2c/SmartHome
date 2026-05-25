using Application.Common.Data;
using Application.Common.Realtime;
using Application.Exceptions;
using Core.Domain.Homes;
using Core.Domain.Scenes;
using MediatR;

namespace Application.Commands.Scenes.CreateScene;

public sealed class CreateSceneCommandHandler : IRequestHandler<CreateSceneCommand, Guid>
{
    private readonly IHomeRepository _homeRepository;
    private readonly ISceneRepository _sceneRepository;
    private readonly IAppReadDbContext _context;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly ICapabilityStateValidator _capabilityStateValidator;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSceneCommandHandler(
        IHomeRepository homeRepository,
        ISceneRepository sceneRepository,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityCommandValidator capabilityCommandValidator,
        ICapabilityStateValidator capabilityStateValidator,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _sceneRepository = sceneRepository;
        _context = context;
        _capabilityRegistry = capabilityRegistry;
        _capabilityCommandValidator = capabilityCommandValidator;
        _capabilityStateValidator = capabilityStateValidator;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSceneCommand request, CancellationToken cancellationToken)
    {
        _ = await _homeRepository.GetById(request.HomeId, cancellationToken)
            ?? throw new HomeNotFoundException(request.HomeId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Scene name is required.");

        var actionSet = await ActionSetValidationHelper.ValidateAndBuildDefinition(
            request.HomeId,
            request.ActionSet,
            _context,
            _capabilityRegistry,
            _capabilityStateValidator,
            _capabilityCommandValidator,
            cancellationToken);

        var scene = Scene.Create(
            request.HomeId,
            request.Name,
            request.Description,
            request.IsEnabled,
            actionSet);

        await _sceneRepository.Add(scene, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtimePublisher.PublishToHome(
            scene.HomeId,
            RealtimeDeltaFactory.ForScene(scene, RealtimeChanges.Created),
            cancellationToken);

        return scene.Id;
    }
}
