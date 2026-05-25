using Application.BusinessServices.ActionSets.Execution;
using Application.BusinessServices.Scenes.Realtime;
using Application.Common.Errors;
using Application.Ports.Persistence;
using Application.Ports.Realtime;
using Domain.Models.ActionSets;
using Domain.Models.Scenes;
using MediatR;

namespace Application.UseCases.Scenes.ExecuteScene;

public sealed class ExecuteSceneCommandHandler : IRequestHandler<ExecuteSceneCommand, Guid>
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IActionSetExecutionRepository _executionRepository;
    private readonly IActionSetProcessor _actionSetProcessor;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public ExecuteSceneCommandHandler(
        ISceneRepository sceneRepository,
        IActionSetExecutionRepository executionRepository,
        IActionSetProcessor actionSetProcessor,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _sceneRepository = sceneRepository;
        _executionRepository = executionRepository;
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

        var execution = ActionSetExecution.Start(
            ActionSetExecutionSource.Scene,
            scene.Id,
            scene.ActionSet.Id,
            scene.HomeId,
            scene.ActionSet.ExecutionMode,
            scene.ActionSet.ContinueOnError,
            scene.ActionSet.Actions);
        await _executionRepository.Add(execution, cancellationToken);

        await _actionSetProcessor.Advance(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishSceneExecutionDelta(execution, cancellationToken);

        return execution.Id;
    }

    private Task PublishSceneExecutionDelta(ActionSetExecution execution, CancellationToken cancellationToken)
    {
        return _realtimePublisher.PublishToHome(
            execution.HomeId,
            SceneRealtime.ForExecution(execution),
            cancellationToken);
    }
}
