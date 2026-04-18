using System.Text.Json;
using Application.Commands.Devices.SendDeviceCommand;
using Application.Commands.Scenes;
using Application.Common.Data;
using Application.Exceptions;
using Application.Services;
using Core.Common;
using Core.Domain.DeviceCommandExecutions;
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
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly ISender _sender;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDeviceCommandResultCommandHandler(
        ILogger<UpdateDeviceCommandResultCommandHandler> logger,
        IDeviceRepository deviceRepository,
        IDeviceCommandExecutionRepository commandExecutionRepository,
        ISceneExecutionRepository sceneExecutionRepository,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityCommandValidator capabilityCommandValidator,
        ISender sender,
        IUnitOfWork unitOfWork
    )
    {
        _logger = logger;
        _deviceRepository = deviceRepository;
        _commandExecutionRepository = commandExecutionRepository;
        _sceneExecutionRepository = sceneExecutionRepository;
        _capabilityRegistry = capabilityRegistry;
        _capabilityCommandValidator = capabilityCommandValidator;
        _sender = sender;
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
                request.CorrelationId
            );
            return;
        }

        var execution = await _commandExecutionRepository.GetByCorrelation(
            request.DeviceId,
            request.CorrelationId
        );

        if (execution is null)
        {
            execution = DeviceCommandExecution.Create(
                deviceId: request.DeviceId,
                capabilityId: request.CapabilityId,
                endpointId: string.Empty,
                correlationId: request.CorrelationId,
                operation: request.Operation,
                requestPayload: null,
                requestedAt: Time.UnixNow()
            );

            await _commandExecutionRepository.Add(execution);
        }

        var payload = SerializePayload(request.Value);

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

        var sceneExecution = await _sceneExecutionRepository.GetByTargetCorrelation(
            request.DeviceId,
            request.CorrelationId,
            cancellationToken);

        if (sceneExecution is not null)
        {
            sceneExecution.TryApplyCommandLifecycle(
                request.DeviceId,
                request.CorrelationId,
                status,
                request.Error);

            if (status == CommandLifecycleStatus.Completed)
            {
                VerifySceneTargetState(sceneExecution, device, request.CorrelationId);
            }

            if (sceneExecution.Status != SceneExecutionStatus.Running)
            {
                await DispatchSideEffectsForTiming(
                    sceneExecution,
                    SceneSideEffectTiming.AfterVerify,
                    new Dictionary<Guid, Device> { [device.Id] = device },
                    cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string? SerializePayload(object? value)
    {
        if (value is null)
            return null;

        return JsonSerializer.Serialize(value);
    }

    private static void VerifySceneTargetState(
        SceneExecution sceneExecution,
        Device device,
        string correlationId)
    {
        var target = sceneExecution.FindTargetByCorrelation(device.Id, correlationId);
        if (target is null)
            return;

        var capability = device.FindCapability(target.CapabilityId, target.EndpointId);
        if (capability is null)
        {
            sceneExecution.MarkTargetFailed(
                target.Id,
                SceneExecutionTargetStatus.CapabilityNotFound,
                $"Capability '{target.CapabilityId}@{target.EndpointId}' was not found while verifying command result.");
            return;
        }

        var desiredState = SceneStateHelper.NormalizeState(target.GetDesiredState());
        if (SceneStateHelper.AreEquivalent(capability.State, desiredState))
        {
            sceneExecution.MarkTargetVerified(target.Id);
            return;
        }

        var unresolvedDiff = SceneStateHelper.BuildUnresolvedDiff(capability.State, desiredState);
        sceneExecution.MarkTargetVerificationFailed(target.Id, unresolvedDiff);
    }

    private async Task DispatchSideEffectsForTiming(
        SceneExecution execution,
        SceneSideEffectTiming timing,
        IDictionary<Guid, Device> deviceCache,
        CancellationToken cancellationToken)
    {
        var sideEffects = execution.FindPendingSideEffects(timing);
        foreach (var sideEffect in sideEffects)
        {
            if (sideEffect.DelayMs > 0)
            {
                await Task.Delay(sideEffect.DelayMs, cancellationToken);
            }

            if (!deviceCache.TryGetValue(sideEffect.DeviceId, out var device))
            {
                device = await _deviceRepository.GetById(sideEffect.DeviceId, cancellationToken);
                if (device is null)
                {
                    execution.MarkSideEffectFailed(
                        sideEffect.Id,
                        $"Side effect device '{sideEffect.DeviceId}' not found.");
                    continue;
                }

                deviceCache[sideEffect.DeviceId] = device;
            }

            var capability = device.FindCapability(sideEffect.CapabilityId, sideEffect.EndpointId);
            if (capability is null)
            {
                execution.MarkSideEffectFailed(
                    sideEffect.Id,
                    $"Side effect capability '{sideEffect.CapabilityId}@{sideEffect.EndpointId}' not found on device '{device.Id}'.");
                continue;
            }

            if (!_capabilityRegistry.TryGetDefinition(
                    capability.CapabilityId,
                    capability.CapabilityVersion,
                    out var definition))
            {
                execution.MarkSideEffectFailed(
                    sideEffect.Id,
                    $"Side effect definition '{capability.CapabilityId}@{capability.CapabilityVersion}' is not found in registry.");
                continue;
            }

            if (!definition.SupportsOperation(sideEffect.Operation))
            {
                execution.MarkSideEffectFailed(
                    sideEffect.Id,
                    $"Side effect operation '{sideEffect.Operation}' is not supported by capability '{capability.CapabilityId}'.");
                continue;
            }

            object? normalizedParams;
            try
            {
                normalizedParams = _capabilityCommandValidator.ValidateAndNormalize(
                    capability,
                    sideEffect.Operation,
                    sideEffect.GetParams());
            }
            catch (Exception ex)
            {
                execution.MarkSideEffectFailed(
                    sideEffect.Id,
                    $"Side effect params are invalid: {ex.Message}");
                continue;
            }

            var correlationId = Guid.NewGuid().ToString("N");

            try
            {
                await _sender.Send(
                    new SendDeviceCommandCommand(
                        device.Id,
                        capability.CapabilityId,
                        sideEffect.EndpointId,
                        sideEffect.Operation,
                        normalizedParams,
                        correlationId),
                    cancellationToken);

                execution.MarkSideEffectSucceeded(sideEffect.Id, correlationId);
            }
            catch (AppException ex)
            {
                execution.MarkSideEffectFailed(sideEffect.Id, ex.Message, correlationId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Error while dispatching AfterVerify side effect {CapabilityId}@{EndpointId} for scene execution {ExecutionId}",
                    sideEffect.CapabilityId,
                    sideEffect.EndpointId,
                    execution.Id);

                execution.MarkSideEffectFailed(sideEffect.Id, ex.Message, correlationId);
            }
        }
    }
}
