using System.Text.Json;
using Application.ActionSets.Planning;
using Application.Common.Data;
using Application.Common.Realtime;
using Application.Exceptions;
using Core.Common;
using Core.Domain.ActionSets;
using Core.Domain.Automations;
using Core.Domain.DeviceCommands;
using Core.Domain.Devices;
using Core.Domain.Scenes;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Devices.UpdateDeviceCommandResult;

public sealed class UpdateDeviceCommandResultCommandHandler
    : IRequestHandler<UpdateDeviceCommandResultCommand>
{
    private readonly ILogger<UpdateDeviceCommandResultCommandHandler> _logger;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDeviceCommandExecutionRepository _commandExecutionRepository;
    private readonly ISceneExecutionRepository _sceneExecutionRepository;
    private readonly IAutomationExecutionRepository _automationExecutionRepository;
    private readonly IActionSetProcessor _actionSetProcessor;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDeviceCommandResultCommandHandler(
        ILogger<UpdateDeviceCommandResultCommandHandler> logger,
        IDeviceRepository deviceRepository,
        IDeviceCommandExecutionRepository commandExecutionRepository,
        ISceneExecutionRepository sceneExecutionRepository,
        IAutomationExecutionRepository automationExecutionRepository,
        IActionSetProcessor actionSetProcessor,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _deviceRepository = deviceRepository;
        _commandExecutionRepository = commandExecutionRepository;
        _sceneExecutionRepository = sceneExecutionRepository;
        _automationExecutionRepository = automationExecutionRepository;
        _actionSetProcessor = actionSetProcessor;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateDeviceCommandResultCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetById(request.DeviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        if (!DeviceCommandExecution.TryParseStatus(request.Status, out var status))
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
            var endpointId = ResolveCommandEndpointId(device, request.CapabilityId, request.EndpointId);

            if (endpointId is null)
            {
                _logger.LogWarning(
                    "Command result {CorrelationId} for device {DeviceId} capability {CapabilityId} has no resolvable endpoint. Command history record will not be created.",
                    request.CorrelationId,
                    request.DeviceId,
                    request.CapabilityId);
            }
            else
            {
                execution = DeviceCommandExecution.Create(
                    deviceId: request.DeviceId,
                    capabilityId: request.CapabilityId,
                    endpointId: endpointId,
                    correlationId: request.CorrelationId,
                    operation: request.Operation,
                    requestPayload: null,
                    requestedAt: Time.UnixNow());

                await _commandExecutionRepository.Add(execution);
            }
        }

        var payload = SerializePayload(request.Value);
        var shouldApplyLifecycleToOwners = execution is null;

        if (execution is not null)
        {
            try
            {
                switch (status)
                {
                    case CommandLifecycleStatus.Accepted:
                        execution.MarkAccepted(payload);
                        break;
                    case CommandLifecycleStatus.Completed:
                        execution.MarkCompleted(payload);
                        break;
                    case CommandLifecycleStatus.Failed:
                        execution.MarkFailed(request.Error ?? "Command failed", payload);
                        break;
                    case CommandLifecycleStatus.TimedOut:
                        execution.MarkTimedOut(request.Error);
                        break;
                    default:
                        execution.ApplyLifecycleResult(status, payload, request.Error);
                        break;
                }

                shouldApplyLifecycleToOwners = true;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Ignoring invalid command lifecycle transition for correlation {CorrelationId}",
                    request.CorrelationId);
            }
        }

        var sceneExecution = await _sceneExecutionRepository.GetByCommandCorrelation(
            request.DeviceId,
            request.CorrelationId,
            cancellationToken);

        if (sceneExecution is not null && shouldApplyLifecycleToOwners)
        {
            sceneExecution.TryApplyCommandLifecycle(
                request.DeviceId,
                request.CorrelationId,
                status,
                request.Error);

            if (status == CommandLifecycleStatus.Completed)
                VerifySceneSetStateAction(sceneExecution, device, request.CorrelationId, request.Value);

            await _actionSetProcessor.AdvanceScene(sceneExecution, cancellationToken);
        }

        var automationExecution = await _automationExecutionRepository.GetByCommandCorrelation(
            request.DeviceId,
            request.CorrelationId,
            cancellationToken);

        if (automationExecution is not null && shouldApplyLifecycleToOwners)
        {
            automationExecution.TryApplyCommandLifecycle(
                request.DeviceId,
                request.CorrelationId,
                status,
                request.Error);

            if (status == CommandLifecycleStatus.Completed)
                VerifyAutomationSetStateAction(automationExecution, device, request.CorrelationId, request.Value);

            await _actionSetProcessor.AdvanceAutomation(automationExecution, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishRealtimeUpdates(
            request.DeviceId,
            execution,
            sceneExecution,
            automationExecution,
            cancellationToken);
    }

    private static string? SerializePayload(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value);
    }

    private static void VerifySceneSetStateAction(
        SceneExecution sceneExecution,
        Device device,
        string correlationId,
        object? resultValue)
    {
        var action = sceneExecution.FindActionByCorrelation(device.Id, correlationId);
        if (action is null || action.Type != ActionType.SetState)
            return;

        var desiredState = ActionStateHelper.NormalizeState(action.GetState());
        if (TryBuildResultState(resultValue, desiredState, out var resultState)
            && ActionStateHelper.AreEquivalent(resultState, desiredState))
        {
            sceneExecution.MarkActionSucceeded(action.Id);
            return;
        }

        var capability = device.FindCapability(action.CapabilityId, action.EndpointId);
        if (capability is null)
        {
            sceneExecution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CapabilityNotFound,
                $"Capability '{action.CapabilityId}@{action.EndpointId}' was not found while verifying command result.");
            return;
        }

        if (ActionStateHelper.AreEquivalent(capability.State, desiredState))
        {
            sceneExecution.MarkActionSucceeded(action.Id);
            return;
        }

        var unresolvedDiff = ActionStateHelper.BuildUnresolvedDiff(capability.State, desiredState);
        sceneExecution.MarkActionVerificationFailed(action.Id, unresolvedDiff);
    }

    private static void VerifyAutomationSetStateAction(
        AutomationExecution automationExecution,
        Device device,
        string correlationId,
        object? resultValue)
    {
        var action = automationExecution.FindActionByCorrelation(device.Id, correlationId);
        if (action is null || action.Type != ActionType.SetState)
            return;

        var desiredState = ActionStateHelper.NormalizeState(action.GetState());
        if (TryBuildResultState(resultValue, desiredState, out var resultState)
            && ActionStateHelper.AreEquivalent(resultState, desiredState))
        {
            automationExecution.MarkActionSucceeded(action.Id);
            return;
        }

        var capability = device.FindCapability(action.CapabilityId, action.EndpointId);
        if (capability is null)
        {
            automationExecution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CapabilityNotFound,
                $"Capability '{action.CapabilityId}@{action.EndpointId}' was not found while verifying command result.");
            return;
        }

        if (ActionStateHelper.AreEquivalent(capability.State, desiredState))
        {
            automationExecution.MarkActionSucceeded(action.Id);
            return;
        }

        var unresolvedDiff = ActionStateHelper.BuildUnresolvedDiff(capability.State, desiredState);
        automationExecution.MarkActionVerificationFailed(action.Id, unresolvedDiff);
    }

    private static bool TryBuildResultState(
        object? resultValue,
        IReadOnlyDictionary<string, object?> desiredState,
        out Dictionary<string, object?> resultState)
    {
        resultState = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var normalized = NormalizeResultValue(resultValue);
        if (normalized is null)
            return false;

        if (TryConvertObjectMap(normalized, out var map))
        {
            if (map.TryGetValue("state", out var stateValue)
                && TryConvertObjectMap(stateValue, out var nestedState))
            {
                resultState = ActionStateHelper.NormalizeState(nestedState);
                return true;
            }

            resultState = ActionStateHelper.NormalizeState(map);
            return true;
        }

        if (desiredState.Count == 1)
        {
            resultState[desiredState.Keys.First()] = normalized;
            return true;
        }

        return false;
    }

    private static object? NormalizeResultValue(object? value)
    {
        return value switch
        {
            JsonElement element => JsonPayloadHelper.ConvertJsonElement(element),
            IReadOnlyDictionary<string, object?> readOnlyMap => readOnlyMap
                .ToDictionary(item => item.Key, item => NormalizeResultValue(item.Value), StringComparer.OrdinalIgnoreCase),
            IDictionary<string, object?> map => map
                .ToDictionary(item => item.Key, item => NormalizeResultValue(item.Value), StringComparer.OrdinalIgnoreCase),
            IEnumerable<object?> list => list.Select(NormalizeResultValue).ToList(),
            _ => value
        };
    }

    private static bool TryConvertObjectMap(
        object? value,
        out Dictionary<string, object?> map)
    {
        if (value is IReadOnlyDictionary<string, object?> readOnlyMap)
        {
            map = readOnlyMap
                .ToDictionary(item => item.Key, item => NormalizeResultValue(item.Value), StringComparer.OrdinalIgnoreCase);
            return true;
        }

        if (value is IDictionary<string, object?> dictionary)
        {
            map = dictionary
                .ToDictionary(item => item.Key, item => NormalizeResultValue(item.Value), StringComparer.OrdinalIgnoreCase);
            return true;
        }

        map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    private static string? ResolveCommandEndpointId(Device device, string capabilityId, string? endpointId)
    {
        if (!string.IsNullOrWhiteSpace(endpointId))
            return endpointId.Trim();

        var matches = device.Endpoints
            .Where(endpoint => endpoint.FindCapability(capabilityId) is not null)
            .Select(endpoint => endpoint.EndpointId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToList();

        return matches.Count == 1 ? matches[0] : null;
    }

    private async Task PublishRealtimeUpdates(
        Guid deviceId,
        DeviceCommandExecution? execution,
        SceneExecution? sceneExecution,
        AutomationExecution? automationExecution,
        CancellationToken cancellationToken)
    {
        await _realtimePublisher.PublishToDevice(
            deviceId,
            execution is null
                ? RealtimeDeltaFactory.ForDeviceCommandExecution(deviceId)
                : RealtimeDeltaFactory.ForDeviceCommandExecution(execution),
            cancellationToken);

        if (sceneExecution is not null)
        {
            await _realtimePublisher.PublishToHome(
                sceneExecution.HomeId,
                RealtimeDeltaFactory.ForSceneExecution(sceneExecution),
                cancellationToken);
        }

        if (automationExecution is not null)
        {
            await _realtimePublisher.PublishToHome(
                automationExecution.HomeId,
                RealtimeDeltaFactory.ForAutomationExecution(automationExecution),
                cancellationToken);
        }
    }
}
