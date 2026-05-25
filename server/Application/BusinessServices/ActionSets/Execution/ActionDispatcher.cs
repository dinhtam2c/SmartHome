using Application.BusinessServices.ActionSets.Planning;
using Application.BusinessServices.Capabilities.Validation;
using Application.Common.Errors;
using Application.Ports.Registries;
using Application.UseCases.Devices.Control.SendCommand;
using Domain.Models.ActionSets;
using Domain.Models.Capabilities;
using Domain.Models.Devices;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.BusinessServices.ActionSets.Execution;

public sealed class ActionDispatcher : IActionDispatcher
{
    private readonly ILogger<ActionDispatcher> _logger;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityCommandValidator _commandValidator;
    private readonly ISetStateActionPlanner _setStatePlanner;
    private readonly ISender _sender;

    public ActionDispatcher(
        ILogger<ActionDispatcher> logger,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityCommandValidator commandValidator,
        ISetStateActionPlanner setStatePlanner,
        ISender sender)
    {
        _logger = logger;
        _capabilityRegistry = capabilityRegistry;
        _commandValidator = commandValidator;
        _setStatePlanner = setStatePlanner;
        _sender = sender;
    }

    public async Task Dispatch(
        ActionSetExecution execution,
        ActionSetActionExecution action,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        switch (action.Type)
        {
            case ActionType.SetState:
                await DispatchSetState(execution, action, devices, autoFixedPrerequisites, cancellationToken);
                break;
            case ActionType.InvokeOperation:
                await DispatchOperation(execution, action, devices, cancellationToken);
                break;
            default:
                execution.MarkActionFailed(action.Id, $"Unsupported action type '{action.Type}'.");
                break;
        }
    }

    private async Task DispatchSetState(
        ActionSetExecution execution,
        ActionSetActionExecution action,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        var command = await BuildSetStateCommand(
            execution.HomeId,
            action,
            devices,
            autoFixedPrerequisites,
            cancellationToken);

        if (command.Outcome == SetStateBuildOutcome.Failed)
        {
            execution.MarkActionFailed(action.Id, command.Error);
            return;
        }

        if (command.Outcome == SetStateBuildOutcome.AlreadySatisfied)
        {
            execution.MarkActionSkipped(action.Id, "Desired state is already satisfied.");
            return;
        }

        await Send(
            execution,
            action,
            command.Device!,
            command.Operation!,
            command.Value,
            cancellationToken);
    }

    private async Task DispatchOperation(
        ActionSetExecution execution,
        ActionSetActionExecution action,
        IReadOnlyDictionary<Guid, Device> devices,
        CancellationToken cancellationToken)
    {
        if (!TryBuildOperationCommand(
                execution.HomeId,
                action,
                devices,
                out var device,
                out var commandValue,
                out var error))
        {
            execution.MarkActionFailed(action.Id, error);
            return;
        }

        await Send(
            execution,
            action,
            device!,
            action.Operation!,
            commandValue,
            cancellationToken);
    }

    private async Task Send(
        ActionSetExecution execution,
        ActionSetActionExecution action,
        Device device,
        string operation,
        object? value,
        CancellationToken cancellationToken)
    {
        var command = SendDeviceCommand.Create(
            device.Id,
            action.CapabilityId,
            action.EndpointId,
            operation,
            value);
        execution.MarkActionWaitingForResult(action.Id, command.CommandExecutionId);

        try
        {
            var commandExecutionId = await _sender.Send(command, cancellationToken);
            execution.MarkActionWaitingForResult(action.Id, commandExecutionId);
        }
        catch (DeviceCommandDispatchException ex)
        {
            execution.MarkActionFailed(action.Id, ex.Message, ex.CommandExecutionId);
        }
        catch (AppException ex)
        {
            execution.MarkActionFailed(action.Id, ex.Message, clearDeviceCommandExecutionId: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error dispatching action {ActionType} {CapabilityId}@{EndpointId} for execution {ExecutionId}",
                action.Type,
                action.CapabilityId,
                action.EndpointId,
                execution.Id);
            execution.MarkActionFailed(action.Id, ex.Message, clearDeviceCommandExecutionId: true);
        }
    }

    private bool TryBuildOperationCommand(
        Guid homeId,
        ActionSetActionExecution action,
        IReadOnlyDictionary<Guid, Device> devices,
        out Device? device,
        out object? commandValue,
        out string? error)
    {
        device = null;
        commandValue = null;

        if (string.IsNullOrWhiteSpace(action.Operation))
        {
            error = "Operation is required.";
            return false;
        }

        if (!TryGetOnlineDevice(homeId, action.DeviceId, devices, out device, out error))
            return false;

        var capability = device!.FindCapability(action.CapabilityId, action.EndpointId);
        if (capability is null)
        {
            error = $"Capability '{action.CapabilityId}@{action.EndpointId}' was not found on device '{device.Id}'.";
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

        if (!definition.SupportsOperation(action.Operation)
            || !capability.SupportsOperation(action.Operation))
        {
            error = $"Operation '{action.Operation}' is not supported by capability '{capability.CapabilityId}'.";
            return false;
        }

        try
        {
            commandValue = _commandValidator.NormalizeAndValidate(
                capability,
                action.Operation,
                action.Payload);
        }
        catch (Exception ex)
        {
            error = $"Operation payload is invalid: {ex.Message}";
            return false;
        }

        error = null;
        return true;
    }

    private async Task<SetStateBuildResult> BuildSetStateCommand(
        Guid homeId,
        ActionSetActionExecution action,
        IReadOnlyDictionary<Guid, Device> devices,
        ISet<string> autoFixedPrerequisites,
        CancellationToken cancellationToken)
    {
        if (!TryGetOnlineDevice(homeId, action.DeviceId, devices, out var device, out var error))
            return SetStateBuildResult.Failed(error);

        var capability = device!.FindCapability(action.CapabilityId, action.EndpointId);
        if (capability is null)
        {
            return SetStateBuildResult.Failed(
                $"Capability '{action.CapabilityId}@{action.EndpointId}' was not found on device '{device.Id}'.");
        }

        if (!_capabilityRegistry.TryGetDefinition(
                capability.CapabilityId,
                capability.CapabilityVersion,
                out var definition))
        {
            return SetStateBuildResult.Failed(
                $"Capability definition '{capability.CapabilityId}@{capability.CapabilityVersion}' is not found in registry.");
        }

        if (definition.Role != CapabilityRole.Control)
        {
            return SetStateBuildResult.Failed(
                $"Capability '{capability.CapabilityId}' has role '{definition.Role}', expected 'Control'.");
        }

        var prerequisite = await EnsurePrerequisiteSatisfied(
            device,
            action.EndpointId,
            definition,
            autoFixedPrerequisites,
            cancellationToken);
        if (!prerequisite.Success)
            return SetStateBuildResult.Failed(prerequisite.Error);

        var desiredState = ActionStateHelper.NormalizeState(action.State);
        if (ActionStateHelper.AreEquivalent(capability.State, desiredState))
            return SetStateBuildResult.Skipped();

        if (!_setStatePlanner.TryPlan(
                new SetStateActionPlanningRequest(capability, definition, desiredState),
                out var plannedCommand,
                out var generationError))
        {
            return SetStateBuildResult.Failed(generationError);
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

        var prerequisite = device.FindCapability(
            definition.Prerequisite.CapabilityId,
            endpointId);
        if (prerequisite is null)
        {
            return (false,
                $"Prerequisite capability '{definition.Prerequisite.CapabilityId}@{endpointId}' was not found on device '{device.Id}'.");
        }

        var requiredState = ActionStateHelper.NormalizeState(definition.Prerequisite.RequiredState);
        if (ActionStateHelper.AreEquivalent(prerequisite.State, requiredState))
            return (true, null);

        if (!definition.Prerequisite.AutoFix)
        {
            return (false,
                $"PrerequisiteNotMet: '{definition.Prerequisite.CapabilityId}' on endpoint '{endpointId}' must match requiredState.");
        }

        var fixKey = BuildPrerequisiteFixKey(
            device.Id,
            endpointId,
            prerequisite.CapabilityId,
            requiredState);
        if (autoFixedPrerequisites.Contains(fixKey))
            return (true, null);

        if (!_capabilityRegistry.TryGetDefinition(
                prerequisite.CapabilityId,
                prerequisite.CapabilityVersion,
                out var prerequisiteDefinition))
        {
            return (false,
                $"Prerequisite capability definition '{prerequisite.CapabilityId}@{prerequisite.CapabilityVersion}' is not found in registry.");
        }

        if (prerequisiteDefinition.Role != CapabilityRole.Control)
        {
            return (false,
                $"Prerequisite capability '{prerequisite.CapabilityId}' is not controllable (role '{prerequisiteDefinition.Role}').");
        }

        if (!_setStatePlanner.TryPlan(
                new SetStateActionPlanningRequest(
                    prerequisite,
                    prerequisiteDefinition,
                    requiredState),
                out var plannedCommand,
                out var generationError))
        {
            return (false,
                $"Cannot auto-fix prerequisite '{prerequisite.CapabilityId}': {generationError}");
        }

        try
        {
            await _sender.Send(
                SendDeviceCommand.Create(
                    device.Id,
                    prerequisite.CapabilityId,
                    endpointId,
                    plannedCommand!.Operation,
                    plannedCommand.Value),
                cancellationToken);
        }
        catch (AppException ex)
        {
            return (false,
                $"Auto-fix prerequisite '{prerequisite.CapabilityId}' dispatch failed: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error auto-fixing prerequisite {CapabilityId} on device {DeviceId}",
                prerequisite.CapabilityId,
                device.Id);
            return (false,
                $"Auto-fix prerequisite '{prerequisite.CapabilityId}' dispatch failed: {ex.Message}");
        }

        autoFixedPrerequisites.Add(fixKey);
        return (true, null);
    }

    private static bool TryGetOnlineDevice(
        Guid homeId,
        Guid deviceId,
        IReadOnlyDictionary<Guid, Device> devices,
        out Device? device,
        out string? error)
    {
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

        error = null;
        return true;
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

    private enum SetStateBuildOutcome
    {
        Command,
        AlreadySatisfied,
        Failed
    }

    private sealed record SetStateBuildResult(
        SetStateBuildOutcome Outcome,
        Device? Device,
        string? Operation,
        object? Value,
        string? Error)
    {
        public static SetStateBuildResult Command(Device device, string operation, object? value) =>
            new(SetStateBuildOutcome.Command, device, operation, value, null);

        public static SetStateBuildResult Skipped() =>
            new(SetStateBuildOutcome.AlreadySatisfied, null, null, null, null);

        public static SetStateBuildResult Failed(string? error) =>
            new(SetStateBuildOutcome.Failed, null, null, null, error);
    }
}
