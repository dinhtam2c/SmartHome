using Application.BusinessServices.ActionSets.Contracts;
using Application.BusinessServices.ActionSets.Planning;
using Application.Ports.Registries;
using Application.BusinessServices.Capabilities.Validation;
using Application.Common.Errors;
using Application.Ports.Persistence;
using Domain.Models.ActionSets;
using Domain.Models.Capabilities;
using Domain.Models.Devices;
using Microsoft.EntityFrameworkCore;

namespace Application.BusinessServices.ActionSets.Validation;

internal static class ActionSetValidationHelper
{
    private sealed record ValidatedAction(
        int Index,
        ActionSetSection Section,
        ActionDefinition Definition,
        CapabilityDefinition CapabilityDefinition);

    public static async Task<ActionSetDefinition> ValidateAndBuildDefinition(
        Guid homeId,
        ActionSetInput? actionSet,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityStateValidator capabilityStateValidator,
        ICapabilityCommandValidator capabilityCommandValidator,
        CancellationToken cancellationToken)
    {
        var mainActions = actionSet?.Actions?.ToList() ?? [];
        var beforeHooks = actionSet?.Hooks?.Before?.ToList() ?? [];
        var successHooks = actionSet?.Hooks?.OnSuccess?.ToList() ?? [];
        var failureHooks = actionSet?.Hooks?.OnFailure?.ToList() ?? [];

        if (mainActions.Count == 0)
            throw new DomainValidationException("actionSet.actions must contain at least one main action.");

        var deviceIds = mainActions
            .Concat(beforeHooks)
            .Concat(successHooks)
            .Concat(failureHooks)
            .Select(action => action.Target?.DeviceId ?? Guid.Empty)
            .Where(deviceId => deviceId != Guid.Empty)
            .Distinct()
            .ToList();

        var devices = await context.Devices
            .AsNoTracking()
            .Include(device => device.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .Where(device => device.HomeId == homeId && deviceIds.Contains(device.Id))
            .ToDictionaryAsync(device => device.Id, cancellationToken);

        var errors = new List<string>();
        var policy = ValidatePolicy(actionSet?.ExecutionPolicy, errors);

        var validatedMain = ValidateActions(
            homeId,
            ActionSetSection.Main,
            mainActions,
            devices,
            capabilityRegistry,
            capabilityStateValidator,
            capabilityCommandValidator,
            errors);

        var validatedBefore = ValidateActions(
            homeId,
            ActionSetSection.Before,
            beforeHooks,
            devices,
            capabilityRegistry,
            capabilityStateValidator,
            capabilityCommandValidator,
            errors);

        var validatedSuccess = ValidateActions(
            homeId,
            ActionSetSection.OnSuccess,
            successHooks,
            devices,
            capabilityRegistry,
            capabilityStateValidator,
            capabilityCommandValidator,
            errors);

        var validatedFailure = ValidateActions(
            homeId,
            ActionSetSection.OnFailure,
            failureHooks,
            devices,
            capabilityRegistry,
            capabilityStateValidator,
            capabilityCommandValidator,
            errors);

        ValidateMainSetStateConflicts(validatedMain, errors);

        if (errors.Count > 0)
            throw new DomainValidationException(string.Join(" | ", errors));

        return new ActionSetDefinition(
            validatedMain.OrderBy(action => action.Index).Select(action => action.Definition).ToList(),
            new ActionSetHooksDefinition(
                validatedBefore.OrderBy(action => action.Index).Select(action => action.Definition).ToList(),
                validatedSuccess.OrderBy(action => action.Index).Select(action => action.Definition).ToList(),
                validatedFailure.OrderBy(action => action.Index).Select(action => action.Definition).ToList()),
            policy);
    }

    private static ActionSetExecutionPolicy ValidatePolicy(
        ActionSetExecutionPolicyInput? policy,
        ICollection<string> errors)
    {
        var mode = ActionExecutionMode.Sequential;
        if (!string.IsNullOrWhiteSpace(policy?.Mode)
            && !TryParseExecutionMode(policy.Mode, out mode))
        {
            errors.Add("actionSet.executionPolicy.mode must be either 'sequential' or 'parallel'.");
        }

        var continueOnError = mode == ActionExecutionMode.Sequential
            && (policy?.ContinueOnError ?? false);

        return new ActionSetExecutionPolicy(mode, continueOnError);
    }

    private static List<ValidatedAction> ValidateActions(
        Guid homeId,
        ActionSetSection section,
        IReadOnlyList<ActionSetActionInput> actions,
        IReadOnlyDictionary<Guid, Device> devices,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityStateValidator capabilityStateValidator,
        ICapabilityCommandValidator capabilityCommandValidator,
        ICollection<string> errors)
    {
        var validatedActions = new List<ValidatedAction>(actions.Count);

        for (var index = 0; index < actions.Count; index++)
        {
            var action = actions[index];
            var prefix = GetPrefix(section, index);

            if (!TryParseActionType(action.Type, out var type))
            {
                errors.Add($"{prefix}.type must be either 'setState' or 'invokeOperation'.");
                continue;
            }

            if (action.Target is null)
            {
                errors.Add($"{prefix}.target is required.");
                continue;
            }

            if (!TryValidateDeviceCapability(
                    homeId,
                    action.Target.DeviceId,
                    action.Target.EndpointId,
                    action.Target.CapabilityId,
                    devices,
                    capabilityRegistry,
                    prefix,
                    errors,
                    out var device,
                    out var capability,
                    out var definition,
                    out var endpointId))
            {
                continue;
            }

            var target = new ActionTarget(device.Id, endpointId, capability.CapabilityId);
            ActionDefinition? definitionAction = type switch
            {
                ActionType.SetState => ValidateSetStateAction(
                    prefix,
                    target,
                    capability,
                    definition,
                    action,
                    capabilityStateValidator,
                    capabilityCommandValidator,
                    errors),
                ActionType.InvokeOperation => ValidateInvokeOperationAction(
                    prefix,
                    target,
                    capability,
                    definition,
                    action,
                    capabilityCommandValidator,
                    errors),
                _ => null
            };

            if (definitionAction is null)
                continue;

            validatedActions.Add(new ValidatedAction(index, section, definitionAction, definition));
        }

        return validatedActions;
    }

    private static SetStateActionDefinition? ValidateSetStateAction(
        string prefix,
        ActionTarget target,
        DeviceCapability capability,
        CapabilityDefinition definition,
        ActionSetActionInput action,
        ICapabilityStateValidator capabilityStateValidator,
        ICapabilityCommandValidator capabilityCommandValidator,
        ICollection<string> errors)
    {
        if (definition.Role != CapabilityRole.Control)
        {
            errors.Add($"{prefix}: setState requires CapabilityRole.Control. '{capability.CapabilityId}' has role '{definition.Role}'.");
            return null;
        }

        var strategy = definition.ApplyStrategy;
        if (strategy is null)
        {
            errors.Add($"{prefix}: capability '{capability.CapabilityId}@{capability.CapabilityVersion}' has no applyStrategy.");
            return null;
        }

        if (action.State is null)
        {
            errors.Add($"{prefix}.state is required.");
            return null;
        }

        var desiredState = ActionStateHelper.NormalizeState(action.State);
        if (desiredState.Count == 0)
        {
            errors.Add($"{prefix}.state must contain at least one field.");
            return null;
        }

        ValidateDesiredState(prefix, capability, definition, desiredState, capabilityStateValidator, errors);
        ValidateSetStateCommandPayload(
            prefix,
            capability,
            strategy,
            desiredState,
            capabilityCommandValidator,
            errors);

        if (action.Operation is not null || action.Payload is not null)
            errors.Add($"{prefix}: setState does not accept operation or payload fields.");

        return new SetStateActionDefinition(target, desiredState);
    }

    private static InvokeOperationActionDefinition? ValidateInvokeOperationAction(
        string prefix,
        ActionTarget target,
        DeviceCapability capability,
        CapabilityDefinition definition,
        ActionSetActionInput action,
        ICapabilityCommandValidator capabilityCommandValidator,
        ICollection<string> errors)
    {
        if (definition.Role == CapabilityRole.Sensor)
        {
            errors.Add($"{prefix}: capability '{capability.CapabilityId}' has role 'Sensor' and cannot be used as invokeOperation.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(action.Operation))
        {
            errors.Add($"{prefix}.operation is required.");
            return null;
        }

        var operation = action.Operation.Trim();
        if (!definition.SupportsOperation(operation) || !capability.SupportsOperation(operation))
        {
            errors.Add($"{prefix}: operation '{operation}' is not supported by capability '{capability.CapabilityId}'.");
            return null;
        }

        Dictionary<string, object?> normalizedPayload;
        try
        {
            var payload = capabilityCommandValidator.NormalizeAndValidate(
                capability,
                operation,
                action.Payload ?? new Dictionary<string, object?>());
            normalizedPayload = ToDictionaryPayload(payload);
        }
        catch (Exception ex)
        {
            errors.Add($"{prefix}.payload for operation '{operation}' is invalid: {ex.Message}");
            return null;
        }

        if (action.State is not null)
            errors.Add($"{prefix}: invokeOperation does not accept state fields.");

        return new InvokeOperationActionDefinition(target, operation, normalizedPayload);
    }

    private static void ValidateDesiredState(
        string prefix,
        DeviceCapability capability,
        CapabilityDefinition definition,
        Dictionary<string, object?> desiredState,
        ICapabilityStateValidator capabilityStateValidator,
        ICollection<string> errors)
    {
        var readOnlyFields = desiredState.Keys
            .Where(field => definition.IsReadOnlyField(field))
            .ToList();
        if (readOnlyFields.Count > 0)
        {
            errors.Add($"{prefix}.state contains readOnly fields for capability '{capability.CapabilityId}': {string.Join(", ", readOnlyFields)}");
        }

        var unsupportedFields = desiredState.Keys
            .Where(field => definition.ApplyStrategy?.StateMapping.ContainsKey(field) != true)
            .ToList();
        if (unsupportedFields.Count > 0)
        {
            errors.Add($"{prefix}.state contains fields not mapped in applyStrategy for capability '{capability.CapabilityId}': {string.Join(", ", unsupportedFields)}");
        }

        if (definition.ApplyStrategy?.PartialUpdate == false)
        {
            var missingFields = definition.ApplyStrategy.StateMapping.Keys
                .Where(mappedField => !desiredState.ContainsKey(mappedField))
                .ToList();
            if (missingFields.Count > 0)
            {
                errors.Add($"{prefix}.state must include full applyStrategy mapping for capability '{capability.CapabilityId}'. Missing: {string.Join(", ", missingFields)}");
            }
        }

        try
        {
            capabilityStateValidator.NormalizeAndValidate(capability, desiredState);
        }
        catch (Exception ex)
        {
            errors.Add($"{prefix}.state for capability '{capability.CapabilityId}' is invalid: {ex.Message}");
        }
    }

    private static void ValidateSetStateCommandPayload(
        string prefix,
        DeviceCapability capability,
        CapabilityApplyStrategyDefinition strategy,
        Dictionary<string, object?> desiredState,
        ICapabilityCommandValidator capabilityCommandValidator,
        ICollection<string> errors)
    {
        var mappedPayloadResult = ActionStateHelper.BuildApplyStrategyPayload(desiredState, strategy);
        if (!mappedPayloadResult.Success)
        {
            errors.Add($"{prefix}: {mappedPayloadResult.Error}");
            return;
        }

        try
        {
            capabilityCommandValidator.NormalizeAndValidate(
                capability,
                strategy.Operation,
                mappedPayloadResult.Payload);
        }
        catch (Exception ex)
        {
            errors.Add($"{prefix}.state produces invalid applyStrategy operation '{strategy.Operation}' payload: {ex.Message}");
        }
    }

    private static bool TryValidateDeviceCapability(
        Guid homeId,
        Guid deviceId,
        string endpointId,
        string capabilityId,
        IReadOnlyDictionary<Guid, Device> devices,
        ICapabilityRegistry capabilityRegistry,
        string prefix,
        ICollection<string> errors,
        out Device device,
        out DeviceCapability capability,
        out CapabilityDefinition definition,
        out string normalizedEndpointId)
    {
        device = default!;
        capability = default!;
        definition = default!;
        normalizedEndpointId = string.Empty;

        if (deviceId == Guid.Empty)
        {
            errors.Add($"{prefix}.target.deviceId is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(endpointId))
        {
            errors.Add($"{prefix}.target.endpointId is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(capabilityId))
        {
            errors.Add($"{prefix}.target.capabilityId is required.");
            return false;
        }

        if (!devices.TryGetValue(deviceId, out device!))
        {
            errors.Add($"{prefix}: device '{deviceId}' is not found in home '{homeId}'.");
            return false;
        }

        normalizedEndpointId = endpointId.Trim();
        capability = device.FindCapability(capabilityId, normalizedEndpointId)!;
        if (capability is null)
        {
            errors.Add($"{prefix}: capability '{capabilityId}@{normalizedEndpointId}' is not found on device '{deviceId}'.");
            return false;
        }

        if (!capabilityRegistry.TryGetDefinition(
                capability.CapabilityId,
                capability.CapabilityVersion,
                out definition!))
        {
            errors.Add($"{prefix}: capability definition '{capability.CapabilityId}@{capability.CapabilityVersion}' is not found in registry.");
            return false;
        }

        return true;
    }

    private static void ValidateMainSetStateConflicts(
        IReadOnlyCollection<ValidatedAction> actions,
        ICollection<string> errors)
    {
        var setStateActions = actions
            .Where(action => action.Definition is SetStateActionDefinition)
            .ToList();

        var duplicateTargets = setStateActions
            .GroupBy(action =>
            {
                var target = action.Definition.Target;
                return (
                    target.DeviceId,
                    EndpointId: target.EndpointId.Trim().ToLowerInvariant(),
                    CapabilityId: target.CapabilityId.Trim().ToLowerInvariant());
            })
            .Where(group => group.Count() > 1)
            .Select(group => group.First())
            .ToList();

        foreach (var duplicate in duplicateTargets)
        {
            errors.Add($"actionSet.actions[{duplicate.Index}] duplicates another setState action for '{duplicate.Definition.Target.CapabilityId}@{duplicate.Definition.Target.EndpointId}'.");
        }

        var byEndpoint = setStateActions
            .GroupBy(action => (
                action.Definition.Target.DeviceId,
                EndpointId: action.Definition.Target.EndpointId.Trim().ToLowerInvariant()))
            .ToList();

        foreach (var group in byEndpoint)
        {
            var endpointTargets = group.OrderBy(action => action.Index).ToList();
            for (var i = 0; i < endpointTargets.Count; i++)
            {
                for (var j = i + 1; j < endpointTargets.Count; j++)
                {
                    var left = endpointTargets[i];
                    var right = endpointTargets[j];

                    if (!left.CapabilityDefinition.ConflictsWithCapability(right.Definition.Target.CapabilityId)
                        && !right.CapabilityDefinition.ConflictsWithCapability(left.Definition.Target.CapabilityId))
                    {
                        continue;
                    }

                    errors.Add($"actionSet.actions[{left.Index}] capability '{left.Definition.Target.CapabilityId}' conflicts with actionSet.actions[{right.Index}] capability '{right.Definition.Target.CapabilityId}' on endpoint '{left.Definition.Target.EndpointId}'.");
                }
            }
        }
    }

    private static Dictionary<string, object?> NormalizeOptionalDictionary(Dictionary<string, object?>? payload)
    {
        return payload is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : ActionStateHelper.NormalizeState(payload);
    }

    private static Dictionary<string, object?> ToDictionaryPayload(object? payload)
    {
        return payload switch
        {
            null => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
            Dictionary<string, object?> dictionary => ActionStateHelper.NormalizeState(dictionary),
            IReadOnlyDictionary<string, object?> readOnlyDictionary =>
                readOnlyDictionary.ToDictionary(
                    item => item.Key,
                    item => item.Value,
                    StringComparer.OrdinalIgnoreCase),
            _ => throw new InvalidOperationException("Action payload must be an object.")
        };
    }

    private static bool TryParseActionType(string? value, out ActionType type)
    {
        if (string.Equals(value, ActionSetWireNames.SetState, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(ActionType.SetState), StringComparison.OrdinalIgnoreCase))
        {
            type = ActionType.SetState;
            return true;
        }

        if (string.Equals(value, ActionSetWireNames.InvokeOperation, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(ActionType.InvokeOperation), StringComparison.OrdinalIgnoreCase))
        {
            type = ActionType.InvokeOperation;
            return true;
        }

        type = default;
        return false;
    }

    private static bool TryParseExecutionMode(string? value, out ActionExecutionMode mode)
    {
        if (string.Equals(value, ActionSetWireNames.Sequential, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(ActionExecutionMode.Sequential), StringComparison.OrdinalIgnoreCase))
        {
            mode = ActionExecutionMode.Sequential;
            return true;
        }

        if (string.Equals(value, ActionSetWireNames.Parallel, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(ActionExecutionMode.Parallel), StringComparison.OrdinalIgnoreCase))
        {
            mode = ActionExecutionMode.Parallel;
            return true;
        }

        mode = default;
        return false;
    }

    private static string GetPrefix(ActionSetSection section, int index)
    {
        return section switch
        {
            ActionSetSection.Main => $"actionSet.actions[{index}]",
            ActionSetSection.Before => $"actionSet.hooks.before[{index}]",
            ActionSetSection.OnSuccess => $"actionSet.hooks.onSuccess[{index}]",
            ActionSetSection.OnFailure => $"actionSet.hooks.onFailure[{index}]",
            _ => $"actionSet.{section}[{index}]"
        };
    }
}
