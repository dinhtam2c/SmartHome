using Application.Common.Data;
using Application.Common.Realtime;
using Application.Exceptions;
using Core.Domain.Scenes;
using MediatR;

namespace Application.Commands.Scenes.ExecuteScene;

public sealed class ExecuteSceneCommandHandler : IRequestHandler<ExecuteSceneCommand, Guid>
{
    private readonly ISceneRepository _sceneRepository;
    private readonly ISceneExecutionRepository _sceneExecutionRepository;
    private readonly IActionSetProcessor _actionSetProcessor;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public ExecuteSceneCommandHandler(
        ISceneRepository sceneRepository,
        ISceneExecutionRepository sceneExecutionRepository,
        IActionSetProcessor actionSetProcessor,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _sceneRepository = sceneRepository;
        _sceneExecutionRepository = sceneExecutionRepository;
        _actionSetProcessor = actionSetProcessor;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(ExecuteSceneCommand request, CancellationToken cancellationToken)
    {
        var scene = await _sceneRepository.GetById(request.SceneId, cancellationToken)
            ?? throw new SceneNotFoundException(request.SceneId);

        if (scene.HomeId != request.HomeId)
            throw new SceneNotFoundException(request.SceneId);

        if (!scene.IsEnabled)
            throw new DomainValidationException($"Scene '{scene.Id}' is disabled.");

        var execution = SceneExecution.Start(scene, request.TriggerSource);
        await _sceneExecutionRepository.Add(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishSceneExecutionDelta(execution, cancellationToken);

        await _actionSetProcessor.AdvanceScene(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishSceneExecutionDelta(execution, cancellationToken);

        return execution.Id;
    }

    private Task PublishSceneExecutionDelta(SceneExecution execution, CancellationToken cancellationToken)
    {
        return _realtimePublisher.PublishToHome(
            execution.HomeId,
            RealtimeDeltaFactory.ForSceneExecution(execution),
            cancellationToken);
    }
}
