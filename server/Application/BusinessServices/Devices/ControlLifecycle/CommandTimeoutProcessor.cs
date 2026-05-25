using Application.BusinessServices.ActionSets.Execution;
using Application.BusinessServices.Automations.Realtime;
using Application.BusinessServices.Scenes.Realtime;
using Application.Ports.Persistence;
using Application.Ports.Realtime;
using Domain.Models.ActionSets;
using Domain.Models.Devices.Commands;

namespace Application.BusinessServices.Devices.ControlLifecycle;

public sealed class CommandTimeoutProcessor
{
    private readonly IDeviceCommandExecutionRepository _commandRepository;
    private readonly IActionSetExecutionRepository _actionSetExecutionRepository;
    private readonly IActionSetProcessor _actionSetProcessor;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public CommandTimeoutProcessor(
        IDeviceCommandExecutionRepository commandRepository,
        IActionSetExecutionRepository actionSetExecutionRepository,
        IActionSetProcessor actionSetProcessor,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _commandRepository = commandRepository;
        _actionSetExecutionRepository = actionSetExecutionRepository;
        _actionSetProcessor = actionSetProcessor;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Process(
        long cutoff,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var staleCommands = (await _commandRepository.GetPendingOlderThan(cutoff, batchSize)).ToList();
        if (staleCommands.Count == 0)
            return 0;

        var changedExecutions = new List<ActionSetExecution>();
        foreach (var command in staleCommands)
        {
            command.MarkTimedOut(command.Error);

            var execution = await _actionSetExecutionRepository.GetByDeviceCommandExecutionId(
                command.Id,
                cancellationToken);
            if (execution is null)
                continue;

            var action = execution.FindActionByDeviceCommandExecutionId(command.Id);
            if (action is not null)
                execution.MarkActionFailed(action.Id, command.Error, command.Id);

            await _actionSetProcessor.Advance(execution, cancellationToken);
            changedExecutions.Add(execution);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var execution in changedExecutions.DistinctBy(execution => execution.Id))
        {
            var delta = execution.SourceType == ActionSetExecutionSource.Scene
                ? SceneRealtime.ForExecution(execution)
                : AutomationRealtime.ForExecution(execution);

            await _realtimePublisher.PublishToHome(
                execution.HomeId,
                delta,
                cancellationToken);
        }

        return staleCommands.Count;
    }
}
