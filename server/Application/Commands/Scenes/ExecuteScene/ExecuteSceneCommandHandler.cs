using Application.Commands.Devices.SendDeviceCommand;
using Application.Common.Data;
using Application.Exceptions;
using Application.Services;
using Core.Domain.Devices;
using Core.Domain.Scenes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Scenes.ExecuteScene;

public sealed class ExecuteSceneCommandHandler : IRequestHandler<ExecuteSceneCommand, Guid>
{
    private readonly ILogger<ExecuteSceneCommandHandler> _logger;
    private readonly ISceneRepository _sceneRepository;
    private readonly ISceneExecutionRepository _sceneExecutionRepository;
    private readonly IAppReadDbContext _context;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly IScenePlanner _scenePlanner;
    private readonly ISender _sender;
    private readonly IUnitOfWork _unitOfWork;

    public ExecuteSceneCommandHandler(
        ILogger<ExecuteSceneCommandHandler> logger,
        ISceneRepository sceneRepository,
        ISceneExecutionRepository sceneExecutionRepository,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityCommandValidator capabilityCommandValidator,
        IScenePlanner scenePlanner,
        ISender sender,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _sceneRepository = sceneRepository;
        _sceneExecutionRepository = sceneExecutionRepository;
        _context = context;
        _capabilityRegistry = capabilityRegistry;
        _capabilityCommandValidator = capabilityCommandValidator;
        _scenePlanner = scenePlanner;
        _sender = sender;
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

        var selectedTargets = execution.Targets
            .OrderBy(target => target.Order)
            .Where(target => IsTargetSelected(target, request.Options))
            .ToList();

        var filteredOutTargetIds = execution.Targets
            .Where(target => !selectedTargets.Any(selected => selected.Id == target.Id))
            .Select(target => target.Id)
            .ToList();

        foreach (var filteredTargetId in filteredOutTargetIds)
        {
            execution.MarkTargetSkipped(filteredTargetId);
        }

        var executionDeviceIds = selectedTargets
            .Select(target => target.DeviceId)
            .Concat(execution.SideEffects.Select(sideEffect => sideEffect.DeviceId))
            .Distinct()
            .ToList();

        var devices = await _context.Devices
            .AsNoTracking()
            .Include(device => device.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .Where(device => device.HomeId == scene.HomeId && executionDeviceIds.Contains(device.Id))
            .ToListAsync(cancellationToken);

        var deviceMap = devices.ToDictionary(device => device.Id);
        var autoFixedPrerequisites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (execution.Targets.Count == 0 && execution.SideEffects.Count > 0)
        {
            await DispatchAllPendingSideEffects(execution, deviceMap, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return execution.Id;
        }

        await DispatchSideEffectsForTiming(
            execution,
            SceneSideEffectTiming.BeforeTargets,
            deviceMap,
            cancellationToken);

        foreach (var target in selectedTargets)
        {
            if (!deviceMap.TryGetValue(target.DeviceId, out var device))
            {
                execution.MarkTargetFailed(
                    target.Id,
                    SceneExecutionTargetStatus.DeviceNotFound,
                    $"Device '{target.DeviceId}' not found in home '{scene.HomeId}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(target.EndpointId))
            {
                execution.MarkTargetFailed(
                    target.Id,
                    SceneExecutionTargetStatus.CommandDispatchFailed,
                    "EndpointId is required for scene target dispatch.");
                continue;
            }

            var endpointId = target.EndpointId;
            var capability = device.FindCapability(target.CapabilityId, endpointId);

            if (capability is null)
            {
                execution.MarkTargetFailed(
                    target.Id,
                    SceneExecutionTargetStatus.CapabilityNotFound,
                    $"Capability '{target.CapabilityId}@{endpointId}' was not found on device '{device.Id}'.");
                continue;
            }

            if (!_capabilityRegistry.TryGetDefinition(
                    capability.CapabilityId,
                    capability.CapabilityVersion,
                    out var definition))
            {
                execution.MarkTargetFailed(
                    target.Id,
                    SceneExecutionTargetStatus.CommandGenerationFailed,
                    $"Capability definition '{capability.CapabilityId}@{capability.CapabilityVersion}' is not found in registry.");
                continue;
            }

            if (definition.Role != CapabilityRole.Control)
            {
                execution.MarkTargetFailed(
                    target.Id,
                    SceneExecutionTargetStatus.UnsupportedCapabilityRole,
                    $"Capability '{capability.CapabilityId}' on device '{device.Id}' has role '{definition.Role}', expected 'Control'.");
                continue;
            }

            var prerequisiteResult = await EnsurePrerequisiteSatisfied(
                device,
                endpointId,
                definition,
                autoFixedPrerequisites,
                cancellationToken);
            if (!prerequisiteResult.Success)
            {
                execution.MarkTargetFailed(
                    target.Id,
                    SceneExecutionTargetStatus.CommandGenerationFailed,
                    prerequisiteResult.Error);
                continue;
            }

            var desiredState = SceneStateHelper.NormalizeState(target.GetDesiredState());
            if (SceneStateHelper.AreEquivalent(capability.State, desiredState))
            {
                execution.MarkTargetSkipped(target.Id);
                continue;
            }

            if (!_scenePlanner.TryPlan(
                    new ScenePlanningRequest(
                        capability,
                        definition,
                        desiredState),
                    out var plannedCommand,
                    out var generationError))
            {
                execution.MarkTargetFailed(
                    target.Id,
                    SceneExecutionTargetStatus.CommandGenerationFailed,
                    generationError);
                continue;
            }

            var correlationId = Guid.NewGuid().ToString("N");
            execution.MarkTargetCommandPending(target.Id, correlationId);

            try
            {
                await _sender.Send(
                    new SendDeviceCommandCommand(
                        device.Id,
                        capability.CapabilityId,
                        endpointId,
                        plannedCommand!.Operation,
                        plannedCommand.Value,
                        correlationId),
                    cancellationToken);
            }
            catch (AppException ex)
            {
                execution.MarkTargetFailed(
                    target.Id,
                    SceneExecutionTargetStatus.CommandDispatchFailed,
                    ex.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Error while dispatching scene {SceneId} target for device {DeviceId} capability {CapabilityId}",
                    scene.Id,
                    device.Id,
                    capability.CapabilityId);

                execution.MarkTargetFailed(
                    target.Id,
                    SceneExecutionTargetStatus.CommandDispatchFailed,
                    ex.Message);
            }
        }

        await DispatchSideEffectsForTiming(
            execution,
            SceneSideEffectTiming.AfterDispatch,
            deviceMap,
            cancellationToken);

        if (execution.PendingTargets == 0)
        {
            await DispatchSideEffectsForTiming(
                execution,
                SceneSideEffectTiming.AfterVerify,
                deviceMap,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return execution.Id;
    }

    private async Task DispatchAllPendingSideEffects(
        SceneExecution execution,
        IReadOnlyDictionary<Guid, Device> deviceMap,
        CancellationToken cancellationToken)
    {
        var pendingSideEffects = execution.SideEffects
            .Where(sideEffect => sideEffect.Status == SceneExecutionSideEffectStatus.Pending)
            .OrderBy(sideEffect => sideEffect.Order)
            .ToList();

        await DispatchSideEffects(execution, pendingSideEffects, deviceMap, cancellationToken);
    }

    private async Task DispatchSideEffectsForTiming(
        SceneExecution execution,
        SceneSideEffectTiming timing,
        IReadOnlyDictionary<Guid, Device> deviceMap,
        CancellationToken cancellationToken)
    {
        var sideEffects = execution.FindPendingSideEffects(timing);
        await DispatchSideEffects(execution, sideEffects, deviceMap, cancellationToken);
    }

    private async Task DispatchSideEffects(
        SceneExecution execution,
        IReadOnlyCollection<SceneExecutionSideEffect> sideEffects,
        IReadOnlyDictionary<Guid, Device> deviceMap,
        CancellationToken cancellationToken)
    {
        foreach (var sideEffect in sideEffects)
        {
            if (sideEffect.DelayMs > 0)
            {
                await Task.Delay(sideEffect.DelayMs, cancellationToken);
            }

            if (!deviceMap.TryGetValue(sideEffect.DeviceId, out var device))
            {
                execution.MarkSideEffectFailed(
                    sideEffect.Id,
                    $"Side effect device '{sideEffect.DeviceId}' not found in home '{execution.HomeId}'.");
                continue;
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
                    "Error while dispatching side effect {CapabilityId}@{EndpointId} for scene execution {ExecutionId}",
                    sideEffect.CapabilityId,
                    sideEffect.EndpointId,
                    execution.Id);

                execution.MarkSideEffectFailed(sideEffect.Id, ex.Message, correlationId);
            }
        }
    }

    private static bool IsTargetSelected(
        SceneExecutionTarget target,
        ExecuteSceneOptions? options)
    {
        if (options is null)
            return true;

        if (options.OnlyEndpoints is { Count: > 0 })
        {
            var containsEndpoint = options.OnlyEndpoints
                .Any(endpoint => endpoint.Equals(target.EndpointId, StringComparison.OrdinalIgnoreCase));

            if (!containsEndpoint)
                return false;
        }

        if (options.ExcludeCapabilities is { Count: > 0 })
        {
            var isExcluded = options.ExcludeCapabilities
                .Any(capability => capability.Equals(target.CapabilityId, StringComparison.OrdinalIgnoreCase));

            if (isExcluded)
                return false;
        }

        return true;
    }

    private async Task<(bool Success, string? Error)> EnsurePrerequisiteSatisfied(
        Device device,
        string endpointId,
        CapabilityDefinition definition,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (definition.Prerequisite is null)
            return (true, null);

        var prerequisiteCapability = device.FindCapability(
            definition.Prerequisite.CapabilityId,
            endpointId);
        if (prerequisiteCapability is null)
        {
            return (false,
                $"Prerequisite capability '{definition.Prerequisite.CapabilityId}@{endpointId}' was not found on device '{device.Id}'.");
        }

        var requiredState = SceneStateHelper.NormalizeState(definition.Prerequisite.RequiredState);
        if (SceneStateHelper.AreEquivalent(prerequisiteCapability.State, requiredState))
            return (true, null);

        if (!definition.Prerequisite.AutoFix)
        {
            return (false,
                $"PrerequisiteNotMet: '{definition.Prerequisite.CapabilityId}' on endpoint '{endpointId}' must match requiredState.");
        }

        var prerequisiteFixKey = BuildPrerequisiteFixKey(
            device.Id,
            endpointId,
            definition.Prerequisite.CapabilityId,
            requiredState);
        if (autoFixedPrerequisites.Contains(prerequisiteFixKey))
            return (true, null);

        if (!_capabilityRegistry.TryGetDefinition(
                prerequisiteCapability.CapabilityId,
                prerequisiteCapability.CapabilityVersion,
                out var prerequisiteDefinition))
        {
            return (false,
                $"Prerequisite capability definition '{prerequisiteCapability.CapabilityId}@{prerequisiteCapability.CapabilityVersion}' is not found in registry.");
        }

        if (prerequisiteDefinition.Role != CapabilityRole.Control)
        {
            return (false,
                $"Prerequisite capability '{prerequisiteCapability.CapabilityId}' is not controllable (role '{prerequisiteDefinition.Role}').");
        }

        if (!_scenePlanner.TryPlan(
                new ScenePlanningRequest(
                    prerequisiteCapability,
                    prerequisiteDefinition,
                    requiredState),
                out var plannedCommand,
                out var generationError))
        {
            return (false,
                $"Cannot auto-fix prerequisite '{prerequisiteCapability.CapabilityId}': {generationError}");
        }

        var correlationId = Guid.NewGuid().ToString("N");

        try
        {
            await _sender.Send(
                new SendDeviceCommandCommand(
                    device.Id,
                    prerequisiteCapability.CapabilityId,
                    endpointId,
                    plannedCommand!.Operation,
                    plannedCommand.Value,
                    correlationId),
                cancellationToken);
        }
        catch (AppException ex)
        {
            return (false,
                $"Auto-fix prerequisite '{prerequisiteCapability.CapabilityId}' dispatch failed: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error while auto-fixing prerequisite {CapabilityId} on device {DeviceId}",
                prerequisiteCapability.CapabilityId,
                device.Id);

            return (false,
                $"Auto-fix prerequisite '{prerequisiteCapability.CapabilityId}' dispatch failed: {ex.Message}");
        }

        autoFixedPrerequisites.Add(prerequisiteFixKey);
        return (true, null);
    }

    private static string BuildPrerequisiteFixKey(
        Guid deviceId,
        string endpointId,
        string capabilityId,
        IReadOnlyDictionary<string, object?> requiredState)
    {
        var stateKey = string.Join(
            ";",
            requiredState
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => $"{item.Key}:{item.Value}"));

        return $"{deviceId:N}|{endpointId.Trim().ToLowerInvariant()}|{capabilityId.Trim().ToLowerInvariant()}|{stateKey}";
    }
}
