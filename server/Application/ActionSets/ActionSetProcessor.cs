using Application.ActionSets.Planning;
using Application.Commands.Devices.SendDeviceCommand;
using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.ActionSets;
using Core.Domain.Automations;
using Core.Domain.Devices;
using Core.Domain.Scenes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.ActionSets;

public sealed class ActionSetProcessor : IActionSetProcessor
{
    private enum AdvanceResult
    {
        Done,
        WaitingForCommand,
        Failed
    }

    private readonly ILogger<ActionSetProcessor> _logger;
    private readonly IAppReadDbContext _context;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly ISetStateActionPlanner _setStateActionPlanner;
    private readonly ISender _sender;

    public ActionSetProcessor(
        ILogger<ActionSetProcessor> logger,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityCommandValidator capabilityCommandValidator,
        ISetStateActionPlanner setStateActionPlanner,
        ISender sender)
    {
        _logger = logger;
        _context = context;
        _capabilityRegistry = capabilityRegistry;
        _capabilityCommandValidator = capabilityCommandValidator;
        _setStateActionPlanner = setStateActionPlanner;
        _sender = sender;
    }

    public async Task AdvanceScene(SceneExecution execution, CancellationToken cancellationToken)
    {
        if (execution.Status != SceneExecutionStatus.Running)
            return;

        var devices = await LoadDevices(
            execution.HomeId,
            execution.Actions.Select(action => action.DeviceId).Distinct().ToList(),
            cancellationToken);
        var autoFixedPrerequisites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (execution.Status == SceneExecutionStatus.Running)
        {
            switch (execution.Phase)
            {
                case ActionExecutionPhase.BeforeHooks:
                    {
                        var result = await RunSceneSectionSequential(
                            execution,
                            ActionSetSection.Before,
                            devices,
                            autoFixedPrerequisites,
                            cancellationToken);

                        if (result == AdvanceResult.WaitingForCommand)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            EnterSceneFailureBranch(execution, "Skipped because a before hook failed.");
                            continue;
                        }

                        execution.EnterPhase(ActionExecutionPhase.MainActions);
                        continue;
                    }
                case ActionExecutionPhase.MainActions:
                    {
                        var result = execution.ExecutionMode == ActionExecutionMode.Parallel
                            ? await RunSceneMainParallel(execution, devices, autoFixedPrerequisites, cancellationToken)
                            : await RunSceneMainSequential(execution, devices, autoFixedPrerequisites, cancellationToken);

                        if (result == AdvanceResult.WaitingForCommand)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            EnterSceneFailureBranch(execution, "Skipped because main actions failed.");
                            continue;
                        }

                        execution.SkipPendingActions(
                            ActionSetSection.OnFailure,
                            "Skipped because main actions succeeded.");
                        execution.EnterPhase(ActionExecutionPhase.OnSuccessHooks);
                        continue;
                    }
                case ActionExecutionPhase.OnSuccessHooks:
                    {
                        var result = await RunSceneSectionSequential(
                            execution,
                            ActionSetSection.OnSuccess,
                            devices,
                            autoFixedPrerequisites,
                            cancellationToken);

                        if (result == AdvanceResult.WaitingForCommand)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            execution.SkipPendingActions(
                                ActionSetSection.OnSuccess,
                                "Skipped because a success hook failed.");
                        }

                        execution.SkipPendingActions(
                            ActionSetSection.OnFailure,
                            "Skipped because success hooks completed.");
                        execution.Complete();
                        return;
                    }
                case ActionExecutionPhase.OnFailureHooks:
                    {
                        var result = await RunSceneSectionSequential(
                            execution,
                            ActionSetSection.OnFailure,
                            devices,
                            autoFixedPrerequisites,
                            cancellationToken);

                        if (result == AdvanceResult.WaitingForCommand)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            execution.SkipPendingActions(
                                ActionSetSection.OnFailure,
                                "Skipped because a failure hook failed.");
                        }

                        execution.Complete();
                        return;
                    }
                case ActionExecutionPhase.Completed:
                    return;
                default:
                    execution.Complete();
                    return;
            }
        }
    }

    public async Task AdvanceAutomation(AutomationExecution execution, CancellationToken cancellationToken)
    {
        if (execution.Status != AutomationExecutionStatus.Running)
            return;

        var devices = await LoadDevices(
            execution.HomeId,
            execution.Actions.Select(action => action.DeviceId).Distinct().ToList(),
            cancellationToken);
        var autoFixedPrerequisites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (execution.Status == AutomationExecutionStatus.Running)
        {
            switch (execution.Phase)
            {
                case ActionExecutionPhase.BeforeHooks:
                    {
                        var result = await RunAutomationSectionSequential(
                            execution,
                            ActionSetSection.Before,
                            devices,
                            autoFixedPrerequisites,
                            cancellationToken);

                        if (result == AdvanceResult.WaitingForCommand)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            EnterAutomationFailureBranch(execution, "Skipped because a before hook failed.");
                            continue;
                        }

                        execution.EnterPhase(ActionExecutionPhase.MainActions);
                        continue;
                    }
                case ActionExecutionPhase.MainActions:
                    {
                        var result = execution.ExecutionMode == ActionExecutionMode.Parallel
                            ? await RunAutomationMainParallel(execution, devices, autoFixedPrerequisites, cancellationToken)
                            : await RunAutomationMainSequential(execution, devices, autoFixedPrerequisites, cancellationToken);

                        if (result == AdvanceResult.WaitingForCommand)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            EnterAutomationFailureBranch(execution, "Skipped because main actions failed.");
                            continue;
                        }

                        execution.SkipPendingActions(
                            ActionSetSection.OnFailure,
                            "Skipped because main actions succeeded.");
                        execution.EnterPhase(ActionExecutionPhase.OnSuccessHooks);
                        continue;
                    }
                case ActionExecutionPhase.OnSuccessHooks:
                    {
                        var result = await RunAutomationSectionSequential(
                            execution,
                            ActionSetSection.OnSuccess,
                            devices,
                            autoFixedPrerequisites,
                            cancellationToken);

                        if (result == AdvanceResult.WaitingForCommand)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            execution.SkipPendingActions(
                                ActionSetSection.OnSuccess,
                                "Skipped because a success hook failed.");
                        }

                        execution.SkipPendingActions(
                            ActionSetSection.OnFailure,
                            "Skipped because success hooks completed.");
                        execution.Complete();
                        return;
                    }
                case ActionExecutionPhase.OnFailureHooks:
                    {
                        var result = await RunAutomationSectionSequential(
                            execution,
                            ActionSetSection.OnFailure,
                            devices,
                            autoFixedPrerequisites,
                            cancellationToken);

                        if (result == AdvanceResult.WaitingForCommand)
                            return;

                        if (result == AdvanceResult.Failed)
                        {
                            execution.SkipPendingActions(
                                ActionSetSection.OnFailure,
                                "Skipped because a failure hook failed.");
                        }

                        execution.Complete();
                        return;
                    }
                case ActionExecutionPhase.Completed:
                    return;
                default:
                    execution.Complete();
                    return;
            }
        }
    }

    private async Task<AdvanceResult> RunSceneSectionSequential(
        SceneExecution execution,
        ActionSetSection section,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (execution.HasActiveAction(section))
            return AdvanceResult.WaitingForCommand;

        if (execution.HasFailedAction(section))
            return AdvanceResult.Failed;

        foreach (var action in execution.FindPendingActions(section))
        {
            await DispatchSceneAction(execution, action, devices, autoFixedPrerequisites, cancellationToken);

            if (IsActive(action.Status))
                return AdvanceResult.WaitingForCommand;

            if (SceneExecutionAction.IsFailureStatus(action.Status))
                return AdvanceResult.Failed;
        }

        return AdvanceResult.Done;
    }

    private async Task<AdvanceResult> RunAutomationSectionSequential(
        AutomationExecution execution,
        ActionSetSection section,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (execution.HasActiveAction(section))
            return AdvanceResult.WaitingForCommand;

        if (execution.HasFailedAction(section))
            return AdvanceResult.Failed;

        foreach (var action in execution.FindPendingActions(section))
        {
            await DispatchAutomationAction(execution, action, devices, autoFixedPrerequisites, cancellationToken);

            if (IsActive(action.Status))
                return AdvanceResult.WaitingForCommand;

            if (AutomationExecutionAction.IsFailureStatus(action.Status))
                return AdvanceResult.Failed;
        }

        return AdvanceResult.Done;
    }

    private async Task<AdvanceResult> RunSceneMainSequential(
        SceneExecution execution,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (execution.HasActiveAction(ActionSetSection.Main))
            return AdvanceResult.WaitingForCommand;

        if (!execution.ContinueOnError && execution.HasFailedAction(ActionSetSection.Main))
            return AdvanceResult.Failed;

        foreach (var action in execution.FindPendingActions(ActionSetSection.Main))
        {
            await DispatchSceneAction(execution, action, devices, autoFixedPrerequisites, cancellationToken);

            if (IsActive(action.Status))
                return AdvanceResult.WaitingForCommand;

            if (SceneExecutionAction.IsFailureStatus(action.Status) && !execution.ContinueOnError)
            {
                execution.SkipPendingActions(
                    ActionSetSection.Main,
                    "Skipped because a previous main action failed.");
                return AdvanceResult.Failed;
            }
        }

        return execution.HasFailedAction(ActionSetSection.Main)
            ? AdvanceResult.Failed
            : AdvanceResult.Done;
    }

    private async Task<AdvanceResult> RunAutomationMainSequential(
        AutomationExecution execution,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (execution.HasActiveAction(ActionSetSection.Main))
            return AdvanceResult.WaitingForCommand;

        if (!execution.ContinueOnError && execution.HasFailedAction(ActionSetSection.Main))
            return AdvanceResult.Failed;

        foreach (var action in execution.FindPendingActions(ActionSetSection.Main))
        {
            await DispatchAutomationAction(execution, action, devices, autoFixedPrerequisites, cancellationToken);

            if (IsActive(action.Status))
                return AdvanceResult.WaitingForCommand;

            if (AutomationExecutionAction.IsFailureStatus(action.Status) && !execution.ContinueOnError)
            {
                execution.SkipPendingActions(
                    ActionSetSection.Main,
                    "Skipped because a previous main action failed.");
                return AdvanceResult.Failed;
            }
        }

        return execution.HasFailedAction(ActionSetSection.Main)
            ? AdvanceResult.Failed
            : AdvanceResult.Done;
    }

    private async Task<AdvanceResult> RunSceneMainParallel(
        SceneExecution execution,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (execution.HasActiveAction(ActionSetSection.Main))
            return AdvanceResult.WaitingForCommand;

        foreach (var action in execution.FindPendingActions(ActionSetSection.Main))
        {
            await DispatchSceneAction(execution, action, devices, autoFixedPrerequisites, cancellationToken);
        }

        if (execution.HasActiveAction(ActionSetSection.Main))
            return AdvanceResult.WaitingForCommand;

        return execution.HasFailedAction(ActionSetSection.Main)
            ? AdvanceResult.Failed
            : AdvanceResult.Done;
    }

    private async Task<AdvanceResult> RunAutomationMainParallel(
        AutomationExecution execution,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (execution.HasActiveAction(ActionSetSection.Main))
            return AdvanceResult.WaitingForCommand;

        foreach (var action in execution.FindPendingActions(ActionSetSection.Main))
        {
            await DispatchAutomationAction(execution, action, devices, autoFixedPrerequisites, cancellationToken);
        }

        if (execution.HasActiveAction(ActionSetSection.Main))
            return AdvanceResult.WaitingForCommand;

        return execution.HasFailedAction(ActionSetSection.Main)
            ? AdvanceResult.Failed
            : AdvanceResult.Done;
    }

    private async Task DispatchSceneAction(
        SceneExecution execution,
        SceneExecutionAction action,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        switch (action.Type)
        {
            case ActionType.SetState:
                await DispatchSceneSetStateAction(
                    execution,
                    action,
                    devices,
                    autoFixedPrerequisites,
                    cancellationToken);
                break;
            case ActionType.InvokeOperation:
                await DispatchSceneInvokeOperationAction(execution, action, devices, cancellationToken);
                break;
            default:
                execution.MarkActionFailed(
                    action.Id,
                    ActionExecutionStatus.Failed,
                    $"Unsupported action type '{action.Type}'.");
                break;
        }
    }

    private async Task DispatchAutomationAction(
        AutomationExecution execution,
        AutomationExecutionAction action,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        switch (action.Type)
        {
            case ActionType.SetState:
                await DispatchAutomationSetStateAction(
                    execution,
                    action,
                    devices,
                    autoFixedPrerequisites,
                    cancellationToken);
                break;
            case ActionType.InvokeOperation:
                await DispatchAutomationInvokeOperationAction(execution, action, devices, cancellationToken);
                break;
            default:
                execution.MarkActionFailed(
                    action.Id,
                    ActionExecutionStatus.Failed,
                    $"Unsupported action type '{action.Type}'.");
                break;
        }
    }

    private async Task DispatchSceneSetStateAction(
        SceneExecution execution,
        SceneExecutionAction action,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        var buildResult = await TryBuildSetStateCommand(
            execution.HomeId,
            action.DeviceId,
            action.EndpointId,
            action.CapabilityId,
            action.GetState(),
            action.GetOptions(),
            devices,
            autoFixedPrerequisites,
            cancellationToken);

        if (buildResult.Status is not null)
        {
            execution.MarkActionFailed(action.Id, buildResult.Status.Value, buildResult.Error);
            return;
        }

        if (buildResult.AlreadySatisfied)
        {
            execution.MarkActionSkippedAlreadySatisfied(action.Id);
            return;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        execution.MarkActionCommandPending(action.Id, correlationId);

        try
        {
            await _sender.Send(
                new SendDeviceCommandCommand(
                    buildResult.Device!.Id,
                    action.CapabilityId,
                    action.EndpointId,
                    buildResult.Operation!,
                    buildResult.Value,
                    correlationId),
                cancellationToken);
        }
        catch (AppException ex)
        {
            execution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CommandDispatchFailed,
                ex.Message,
                correlationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error while dispatching scene setState action {CapabilityId}@{EndpointId} for execution {ExecutionId}",
                action.CapabilityId,
                action.EndpointId,
                execution.Id);

            execution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CommandDispatchFailed,
                ex.Message,
                correlationId);
        }
    }

    private async Task DispatchAutomationSetStateAction(
        AutomationExecution execution,
        AutomationExecutionAction action,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        var buildResult = await TryBuildSetStateCommand(
            execution.HomeId,
            action.DeviceId,
            action.EndpointId,
            action.CapabilityId,
            action.GetState(),
            action.GetOptions(),
            devices,
            autoFixedPrerequisites,
            cancellationToken);

        if (buildResult.Status is not null)
        {
            execution.MarkActionFailed(action.Id, buildResult.Status.Value, buildResult.Error);
            return;
        }

        if (buildResult.AlreadySatisfied)
        {
            execution.MarkActionSkippedAlreadySatisfied(action.Id);
            return;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        execution.MarkActionCommandPending(action.Id, correlationId);

        try
        {
            await _sender.Send(
                new SendDeviceCommandCommand(
                    buildResult.Device!.Id,
                    action.CapabilityId,
                    action.EndpointId,
                    buildResult.Operation!,
                    buildResult.Value,
                    correlationId),
                cancellationToken);
        }
        catch (AppException ex)
        {
            execution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CommandDispatchFailed,
                ex.Message,
                correlationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error while dispatching automation setState action {CapabilityId}@{EndpointId} for execution {ExecutionId}",
                action.CapabilityId,
                action.EndpointId,
                execution.Id);

            execution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CommandDispatchFailed,
                ex.Message,
                correlationId);
        }
    }

    private async Task DispatchSceneInvokeOperationAction(
        SceneExecution execution,
        SceneExecutionAction action,
        IReadOnlyDictionary<Guid, Device> devices,
        CancellationToken cancellationToken)
    {
        if (!TryBuildOperationCommand(
                execution.HomeId,
                action.DeviceId,
                action.EndpointId,
                action.CapabilityId,
                action.Operation,
                action.GetPayload(),
                devices,
                out var device,
                out var commandValue,
                out var error))
        {
            execution.MarkActionFailed(action.Id, ActionExecutionStatus.CommandGenerationFailed, error);
            return;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        execution.MarkActionCommandPending(action.Id, correlationId);

        try
        {
            await _sender.Send(
                new SendDeviceCommandCommand(
                    device!.Id,
                    action.CapabilityId,
                    action.EndpointId,
                    action.Operation!,
                    commandValue,
                    correlationId),
                cancellationToken);
        }
        catch (AppException ex)
        {
            execution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CommandDispatchFailed,
                ex.Message,
                correlationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error while dispatching scene invokeOperation action {CapabilityId}@{EndpointId} for execution {ExecutionId}",
                action.CapabilityId,
                action.EndpointId,
                execution.Id);

            execution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CommandDispatchFailed,
                ex.Message,
                correlationId);
        }
    }

    private async Task DispatchAutomationInvokeOperationAction(
        AutomationExecution execution,
        AutomationExecutionAction action,
        IReadOnlyDictionary<Guid, Device> devices,
        CancellationToken cancellationToken)
    {
        if (!TryBuildOperationCommand(
                execution.HomeId,
                action.DeviceId,
                action.EndpointId,
                action.CapabilityId,
                action.Operation,
                action.GetPayload(),
                devices,
                out var device,
                out var commandValue,
                out var error))
        {
            execution.MarkActionFailed(action.Id, ActionExecutionStatus.CommandGenerationFailed, error);
            return;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        execution.MarkActionCommandPending(action.Id, correlationId);

        try
        {
            await _sender.Send(
                new SendDeviceCommandCommand(
                    device!.Id,
                    action.CapabilityId,
                    action.EndpointId,
                    action.Operation!,
                    commandValue,
                    correlationId),
                cancellationToken);
        }
        catch (AppException ex)
        {
            execution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CommandDispatchFailed,
                ex.Message,
                correlationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error while dispatching automation invokeOperation action {CapabilityId}@{EndpointId} for execution {ExecutionId}",
                action.CapabilityId,
                action.EndpointId,
                execution.Id);

            execution.MarkActionFailed(
                action.Id,
                ActionExecutionStatus.CommandDispatchFailed,
                ex.Message,
                correlationId);
        }
    }

    private bool TryBuildOperationCommand(
        Guid homeId,
        Guid deviceId,
        string endpointId,
        string capabilityId,
        string? operation,
        Dictionary<string, object?> payload,
        IReadOnlyDictionary<Guid, Device> devices,
        out Device? device,
        out object? commandValue,
        out string? error)
    {
        device = null;
        commandValue = null;

        if (string.IsNullOrWhiteSpace(operation))
        {
            error = "Operation is required.";
            return false;
        }

        if (!devices.TryGetValue(deviceId, out device))
        {
            error = $"Device '{deviceId}' not found in home '{homeId}'.";
            return false;
        }

        if (!device.IsOnline)
        {
            error = $"Device '{deviceId}' is offline.";
            return false;
        }

        var capability = device.FindCapability(capabilityId, endpointId);
        if (capability is null)
        {
            error = $"Capability '{capabilityId}@{endpointId}' was not found on device '{device.Id}'.";
            return false;
        }

        if (!_capabilityRegistry.TryGetDefinition(
                capability.CapabilityId,
                capability.CapabilityVersion,
                out var definition))
        {
            error = $"Capability definition '{capability.CapabilityId}@{capability.CapabilityVersion}' is not found in registry.";
            return false;
        }

        if (definition.Role == CapabilityRole.Sensor)
        {
            error = $"Capability '{capability.CapabilityId}' has role 'Sensor' and cannot run operations.";
            return false;
        }

        if (!definition.SupportsOperation(operation) || !capability.SupportsOperation(operation))
        {
            error = $"Operation '{operation}' is not supported by capability '{capability.CapabilityId}'.";
            return false;
        }

        try
        {
            commandValue = _capabilityCommandValidator.ValidateAndNormalize(capability, operation, payload);
        }
        catch (Exception ex)
        {
            error = $"Operation payload is invalid: {ex.Message}";
            return false;
        }

        error = null;
        return true;
    }

    private async Task<SetStateBuildResult> TryBuildSetStateCommand(
        Guid homeId,
        Guid deviceId,
        string endpointId,
        string capabilityId,
        Dictionary<string, object?> statePayload,
        Dictionary<string, object?> optionsPayload,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (!devices.TryGetValue(deviceId, out var device))
        {
            return SetStateBuildResult.Failed(
                ActionExecutionStatus.DeviceNotFound,
                $"Device '{deviceId}' not found in home '{homeId}'.");
        }

        if (!device.IsOnline)
        {
            return SetStateBuildResult.Failed(
                ActionExecutionStatus.DeviceOffline,
                $"Device '{deviceId}' is offline.");
        }

        var capability = device.FindCapability(capabilityId, endpointId);
        if (capability is null)
        {
            return SetStateBuildResult.Failed(
                ActionExecutionStatus.CapabilityNotFound,
                $"Capability '{capabilityId}@{endpointId}' was not found on device '{device.Id}'.");
        }

        if (!_capabilityRegistry.TryGetDefinition(
                capability.CapabilityId,
                capability.CapabilityVersion,
                out var definition))
        {
            return SetStateBuildResult.Failed(
                ActionExecutionStatus.CommandGenerationFailed,
                $"Capability definition '{capability.CapabilityId}@{capability.CapabilityVersion}' is not found in registry.");
        }

        if (definition.Role != CapabilityRole.Control)
        {
            return SetStateBuildResult.Failed(
                ActionExecutionStatus.UnsupportedCapabilityRole,
                $"Capability '{capability.CapabilityId}' has role '{definition.Role}', expected 'Control'.");
        }

        var prerequisiteResult = await EnsurePrerequisiteSatisfied(
            device,
            endpointId,
            definition,
            autoFixedPrerequisites,
            cancellationToken);
        if (!prerequisiteResult.Success)
        {
            return SetStateBuildResult.Failed(
                ActionExecutionStatus.CommandGenerationFailed,
                prerequisiteResult.Error);
        }

        var desiredState = ActionStateHelper.NormalizeState(statePayload);
        var options = ActionStateHelper.NormalizeState(optionsPayload);
        if (ActionStateHelper.AreEquivalent(capability.State, desiredState))
            return SetStateBuildResult.Skipped(device);

        if (!_setStateActionPlanner.TryPlan(
                new SetStateActionPlanningRequest(
                    capability,
                    definition,
                    desiredState,
                    options),
                out var plannedCommand,
                out var generationError))
        {
            return SetStateBuildResult.Failed(
                ActionExecutionStatus.CommandGenerationFailed,
                generationError);
        }

        return SetStateBuildResult.Command(
            device,
            plannedCommand!.Operation,
            plannedCommand.Value);
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

        var requiredState = ActionStateHelper.NormalizeState(definition.Prerequisite.RequiredState);
        if (ActionStateHelper.AreEquivalent(prerequisiteCapability.State, requiredState))
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

        if (!_setStateActionPlanner.TryPlan(
                new SetStateActionPlanningRequest(
                    prerequisiteCapability,
                    prerequisiteDefinition,
                    requiredState,
                    []),
                out var plannedCommand,
                out var generationError))
        {
            return (false,
                $"Cannot auto-fix prerequisite '{prerequisiteCapability.CapabilityId}': {generationError}");
        }

        try
        {
            await _sender.Send(
                new SendDeviceCommandCommand(
                    device.Id,
                    prerequisiteCapability.CapabilityId,
                    endpointId,
                    plannedCommand!.Operation,
                    plannedCommand.Value,
                    Guid.NewGuid().ToString("N")),
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

    private async Task<Dictionary<Guid, Device>> LoadDevices(
        Guid homeId,
        IReadOnlyCollection<Guid> deviceIds,
        CancellationToken cancellationToken)
    {
        if (deviceIds.Count == 0)
            return [];

        return await _context.Devices
            .Include(device => device.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .Where(device => device.HomeId == homeId && deviceIds.Contains(device.Id))
            .ToDictionaryAsync(device => device.Id, cancellationToken);
    }

    private static bool IsActive(ActionExecutionStatus status)
    {
        return status is ActionExecutionStatus.CommandPending or ActionExecutionStatus.CommandAccepted;
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

    private static void EnterSceneFailureBranch(SceneExecution execution, string reason)
    {
        execution.SkipPendingActions(ActionSetSection.Before, reason);
        execution.SkipPendingActions(ActionSetSection.Main, reason);
        execution.SkipPendingActions(ActionSetSection.OnSuccess, reason);
        execution.EnterPhase(ActionExecutionPhase.OnFailureHooks);
    }

    private static void EnterAutomationFailureBranch(AutomationExecution execution, string reason)
    {
        execution.SkipPendingActions(ActionSetSection.Before, reason);
        execution.SkipPendingActions(ActionSetSection.Main, reason);
        execution.SkipPendingActions(ActionSetSection.OnSuccess, reason);
        execution.EnterPhase(ActionExecutionPhase.OnFailureHooks);
    }

    private sealed record SetStateBuildResult(
        Device? Device,
        bool AlreadySatisfied,
        string? Operation,
        object? Value,
        ActionExecutionStatus? Status,
        string? Error)
    {
        public static SetStateBuildResult Command(Device device, string operation, object? value)
        {
            return new SetStateBuildResult(device, false, operation, value, null, null);
        }

        public static SetStateBuildResult Skipped(Device device)
        {
            return new SetStateBuildResult(device, true, null, null, null, null);
        }

        public static SetStateBuildResult Failed(ActionExecutionStatus status, string? error)
        {
            return new SetStateBuildResult(null, false, null, null, status, error);
        }
    }
}
