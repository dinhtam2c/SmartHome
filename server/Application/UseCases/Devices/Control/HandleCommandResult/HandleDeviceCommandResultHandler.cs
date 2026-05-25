using System.Text.Json;
using Application.BusinessServices.ActionSets.Execution;
using Application.BusinessServices.Automations.Realtime;
using Application.BusinessServices.Devices.State;
using Application.BusinessServices.Scenes.Realtime;
using Application.Ports.Persistence;
using Application.Ports.Realtime;
using Domain.Models.ActionSets;
using Domain.Models.Devices.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Devices.Control.HandleCommandResult;

public sealed class HandleDeviceCommandResultHandler
    : IRequestHandler<HandleDeviceCommandResult>
{
    private readonly ILogger<HandleDeviceCommandResultHandler> _logger;
    private readonly IDeviceCommandExecutionRepository _commandExecutionRepository;
    private readonly IActionSetExecutionRepository _actionSetExecutionRepository;
    private readonly IActionSetProcessor _actionSetProcessor;
    private readonly ICapabilityStateUpdater _capabilityStateUpdater;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public HandleDeviceCommandResultHandler(
        ILogger<HandleDeviceCommandResultHandler> logger,
        IDeviceCommandExecutionRepository commandExecutionRepository,
        IActionSetExecutionRepository actionSetExecutionRepository,
        IActionSetProcessor actionSetProcessor,
        ICapabilityStateUpdater capabilityStateUpdater,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _commandExecutionRepository = commandExecutionRepository;
        _actionSetExecutionRepository = actionSetExecutionRepository;
        _actionSetProcessor = actionSetProcessor;
        _capabilityStateUpdater = capabilityStateUpdater;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(HandleDeviceCommandResult request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EndpointId))
        {
            _logger.LogWarning(
                "Ignoring command result {CorrelationId} for device {DeviceId}: endpointId is required.",
                request.CorrelationId,
                request.DeviceId);
            return;
        }

        var endpointId = request.EndpointId.Trim();

        if (!DeviceCommandExecution.TryParseResultStatus(request.Status, out var status))
        {
            _logger.LogWarning(
                "Ignoring command result with invalid status {Status} for correlation {CorrelationId}",
                request.Status,
                request.CorrelationId);
            return;
        }

        var execution = await _commandExecutionRepository.GetByCorrelation(
            request.DeviceId,
            request.CorrelationId);

        if (execution is null)
        {
            _logger.LogWarning(
                "Ignoring orphan command result {CorrelationId} for device {DeviceId}.",
                request.CorrelationId,
                request.DeviceId);
            return;
        }

        if (!MatchesExecution(execution, request, endpointId))
        {
            _logger.LogWarning(
                "Ignoring command result {CorrelationId} for device {DeviceId}: target does not match the original command.",
                request.CorrelationId,
                request.DeviceId);
            return;
        }

        var payload = SerializePayload(request.StateChanges);
        var shouldApplyOutcomeToActions = false;

        try
        {
            switch (status)
            {
                case CommandLifecycleStatus.Completed:
                    execution.MarkCompleted(payload);
                    break;
                case CommandLifecycleStatus.Failed:
                    execution.MarkFailed(request.Error ?? "Command failed", payload);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Command result status '{status}' is not supported.");
            }

            shouldApplyOutcomeToActions = true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Ignoring invalid command lifecycle transition for correlation {CorrelationId}",
                request.CorrelationId);
        }

        IReadOnlyList<CapabilityStateUpdate> appliedStateChanges = [];
        if (shouldApplyOutcomeToActions && status == CommandLifecycleStatus.Completed)
        {
            appliedStateChanges = await _capabilityStateUpdater.Apply(
                request.DeviceId,
                request.StateChanges,
                cancellationToken);
        }

        var actionSetExecution = await _actionSetExecutionRepository.GetByDeviceCommandExecutionId(
            execution.Id,
            cancellationToken);

        if (actionSetExecution is not null && shouldApplyOutcomeToActions)
        {
            ActionOutcomeResolver.ApplyCommandResult(
                actionSetExecution,
                execution.Id,
                status,
                appliedStateChanges,
                request.Error);

            await _actionSetProcessor.Advance(actionSetExecution, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishActionSetExecution(actionSetExecution, cancellationToken);
    }

    private static string? SerializePayload(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value);
    }

    private static bool MatchesExecution(
        DeviceCommandExecution execution,
        HandleDeviceCommandResult result,
        string endpointId)
    {
        return execution.EndpointId.Equals(endpointId, StringComparison.OrdinalIgnoreCase)
            && execution.CapabilityId.Equals(result.CapabilityId, StringComparison.OrdinalIgnoreCase)
            && execution.Operation.Equals(result.Operation, StringComparison.OrdinalIgnoreCase);
    }

    private async Task PublishActionSetExecution(
        ActionSetExecution? actionSetExecution,
        CancellationToken cancellationToken)
    {
        if (actionSetExecution is null)
            return;

        var delta = actionSetExecution.SourceType == ActionSetExecutionSource.Scene
            ? SceneRealtime.ForExecution(actionSetExecution)
            : AutomationRealtime.ForExecution(actionSetExecution);

        await _realtimePublisher.PublishToHome(
            actionSetExecution.HomeId,
            delta,
            cancellationToken);
    }
}
