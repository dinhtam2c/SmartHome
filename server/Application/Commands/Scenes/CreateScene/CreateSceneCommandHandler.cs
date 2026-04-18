using Application.Common.Data;
using Application.Exceptions;
using Application.Services;
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
    private readonly IUnitOfWork _unitOfWork;

    public CreateSceneCommandHandler(
        IHomeRepository homeRepository,
        ISceneRepository sceneRepository,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityCommandValidator capabilityCommandValidator,
        ICapabilityStateValidator capabilityStateValidator,
        IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _sceneRepository = sceneRepository;
        _context = context;
        _capabilityRegistry = capabilityRegistry;
        _capabilityCommandValidator = capabilityCommandValidator;
        _capabilityStateValidator = capabilityStateValidator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSceneCommand request, CancellationToken cancellationToken)
    {
        _ = await _homeRepository.GetById(request.HomeId, cancellationToken)
            ?? throw new HomeNotFoundException(request.HomeId);

        var targetDefinitions = await SceneTargetValidationHelper.ValidateAndBuildDefinitions(
            request.HomeId,
            request.Targets,
            _context,
            _capabilityRegistry,
            _capabilityStateValidator,
            cancellationToken);

        var sideEffectDefinitions = await SceneTargetValidationHelper.ValidateAndBuildSideEffectDefinitions(
            request.HomeId,
            targetDefinitions,
            request.SideEffects,
            _context,
            _capabilityRegistry,
            _capabilityCommandValidator,
            cancellationToken);

        if (targetDefinitions.Count == 0 && sideEffectDefinitions.Count == 0)
            throw new DomainValidationException("Scene must contain at least one target or side effect.");

        var scene = Scene.Create(
            request.HomeId,
            request.Name,
            request.Description,
            request.IsEnabled,
            targetDefinitions,
            sideEffectDefinitions);

        await _sceneRepository.Add(scene, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return scene.Id;
    }
}
