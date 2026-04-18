using Application.Common.Data;
using Application.Exceptions;
using Application.Services;
using Core.Domain.Devices;
using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;

namespace Application.Commands.Scenes;

internal static class SceneTargetValidationHelper
{
    private sealed record ValidatedTarget(
        int Index,
        Guid DeviceId,
        string EndpointId,
        string CapabilityId,
        CapabilityDefinition Definition,
        Dictionary<string, object?> DesiredState);

    private sealed record ValidatedSideEffect(
        int Index,
        Guid DeviceId,
        string EndpointId,
        string CapabilityId,
        string Operation,
        Dictionary<string, object?> Params,
        SceneSideEffectTiming Timing,
        int DelayMs);

    public static async Task<List<SceneTargetDefinition>> ValidateAndBuildDefinitions(
        Guid homeId,
        IEnumerable<SceneTargetModel>? targets,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityStateValidator capabilityStateValidator,
        CancellationToken cancellationToken)
    {
        if (targets is null)
            return [];

        var targetList = targets.ToList();
        if (targetList.Count == 0)
            return [];

        var errors = new List<string>();

        var duplicateTargets = targetList
            .GroupBy(
                target => (
                    target.DeviceId,
                    CapabilityId: target.CapabilityId?.Trim() ?? string.Empty,
                    EndpointId: target.EndpointId?.Trim() ?? string.Empty),
                SceneTargetKeyComparer.Instance)
            .Where(group => group.Count() > 1)
            .Select(group =>
                $"{group.Key.DeviceId}:{group.Key.CapabilityId}@{group.Key.EndpointId}")
            .ToList();

        if (duplicateTargets.Count > 0)
        {
            errors.Add($"Scene targets contain duplicate entries: {string.Join(", ", duplicateTargets)}");
        }

        var deviceIds = targetList
            .Select(target => target.DeviceId)
            .Where(deviceId => deviceId != Guid.Empty)
            .Distinct()
            .ToList();

        var devices = await context.Devices
            .AsNoTracking()
            .Include(device => device.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .Where(device => device.HomeId == homeId && deviceIds.Contains(device.Id))
            .ToListAsync(cancellationToken);

        var deviceMap = devices.ToDictionary(device => device.Id);
        var validatedTargets = new List<ValidatedTarget>(targetList.Count);

        for (var index = 0; index < targetList.Count; index++)
        {
            var target = targetList[index];
            var errorPrefix = $"targets[{index}]";

            if (target.DeviceId == Guid.Empty)
            {
                errors.Add($"{errorPrefix}.deviceId is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(target.CapabilityId))
            {
                errors.Add($"{errorPrefix}.capabilityId is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(target.EndpointId))
            {
                errors.Add($"{errorPrefix}.endpointId is required.");
                continue;
            }

            if (!deviceMap.TryGetValue(target.DeviceId, out var device))
            {
                errors.Add($"{errorPrefix}: device '{target.DeviceId}' is not found in home '{homeId}'.");
                continue;
            }

            var normalizedEndpointId = target.EndpointId.Trim();
            var capability = device.FindCapability(target.CapabilityId, normalizedEndpointId);

            if (capability is null)
            {
                errors.Add(
                    $"{errorPrefix}: capability '{target.CapabilityId}@{normalizedEndpointId}' is not found on device '{target.DeviceId}'.");
                continue;
            }

            if (!capabilityRegistry.TryGetDefinition(
                capability.CapabilityId,
                capability.CapabilityVersion,
                out var definition))
            {
                errors.Add(
                    $"{errorPrefix}: capability definition '{capability.CapabilityId}@{capability.CapabilityVersion}' is not found in registry.");
                continue;
            }

            if (definition.Role != CapabilityRole.Control)
            {
                errors.Add(
                    $"{errorPrefix}: scene can control only CapabilityRole.Control. '{capability.CapabilityId}' on device '{device.Id}' has role '{definition.Role}'.");
                continue;
            }

            if (definition.ApplyStrategy is null)
            {
                errors.Add(
                    $"{errorPrefix}: capability '{capability.CapabilityId}@{capability.CapabilityVersion}' has no applyStrategy.");
                continue;
            }

            if (target.DesiredState is null)
            {
                errors.Add($"{errorPrefix}.desiredState is required.");
                continue;
            }

            var normalizedDesiredState = SceneStateHelper.NormalizeState(target.DesiredState);
            if (normalizedDesiredState.Count == 0)
            {
                errors.Add($"{errorPrefix}.desiredState must contain at least one field.");
                continue;
            }

            var readOnlyFieldsInDesiredState = normalizedDesiredState.Keys
                .Where(field => definition.IsReadOnlyField(field))
                .ToList();
            if (readOnlyFieldsInDesiredState.Count > 0)
            {
                errors.Add(
                    $"{errorPrefix}.desiredState contains readOnly fields for capability '{capability.CapabilityId}': {string.Join(", ", readOnlyFieldsInDesiredState)}");
            }

            var unsupportedDesiredFields = normalizedDesiredState.Keys
                .Where(field => !definition.ApplyStrategy.StateMapping.ContainsKey(field))
                .ToList();
            if (unsupportedDesiredFields.Count > 0)
            {
                errors.Add(
                    $"{errorPrefix}.desiredState contains fields not mapped in applyStrategy for capability '{capability.CapabilityId}': {string.Join(", ", unsupportedDesiredFields)}");
            }

            if (!definition.ApplyStrategy.PartialUpdate)
            {
                var missingMappedFields = definition.ApplyStrategy.StateMapping.Keys
                    .Where(mappedField => !normalizedDesiredState.ContainsKey(mappedField))
                    .ToList();

                if (missingMappedFields.Count > 0)
                {
                    errors.Add(
                        $"{errorPrefix}.desiredState must include full applyStrategy mapping for capability '{capability.CapabilityId}' (partialUpdate=false). Missing: {string.Join(", ", missingMappedFields)}");
                }
            }

            try
            {
                capabilityStateValidator.Validate(capability, normalizedDesiredState);
            }
            catch (Exception ex)
            {
                errors.Add(
                    $"{errorPrefix}.desiredState for capability '{capability.CapabilityId}@{normalizedEndpointId}' is invalid: {ex.Message}");
            }

            validatedTargets.Add(new ValidatedTarget(
                index,
                device.Id,
                normalizedEndpointId,
                capability.CapabilityId,
                definition,
                normalizedDesiredState));
        }

        ValidateCapabilityConflicts(validatedTargets, errors);

        if (errors.Count > 0)
        {
            throw new DomainValidationException(string.Join(" | ", errors));
        }

        return validatedTargets
            .OrderBy(item => item.Index)
            .Select(item => new SceneTargetDefinition(
                item.DeviceId,
                item.EndpointId,
                item.CapabilityId,
                item.DesiredState))
            .ToList();
    }

    public static async Task<List<SceneSideEffectDefinition>> ValidateAndBuildSideEffectDefinitions(
        Guid homeId,
        IReadOnlyCollection<SceneTargetDefinition> targets,
        IEnumerable<SceneSideEffectModel>? sideEffects,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityCommandValidator capabilityCommandValidator,
        CancellationToken cancellationToken)
    {
        if (sideEffects is null)
            return [];

        var sideEffectList = sideEffects.ToList();
        if (sideEffectList.Count == 0)
            return [];

        var errors = new List<string>();
        var targetKeys = targets
            .Select(target => ToTargetKey(target.DeviceId, target.EndpointId, target.CapabilityId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var deviceIds = sideEffectList
            .Select(sideEffect => sideEffect.DeviceId)
            .Where(deviceId => deviceId != Guid.Empty)
            .Distinct()
            .ToList();

        var devices = await context.Devices
            .AsNoTracking()
            .Include(device => device.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .Where(device => device.HomeId == homeId && deviceIds.Contains(device.Id))
            .ToListAsync(cancellationToken);

        var deviceMap = devices.ToDictionary(device => device.Id);
        var validatedSideEffects = new List<ValidatedSideEffect>(sideEffectList.Count);

        for (var index = 0; index < sideEffectList.Count; index++)
        {
            var sideEffect = sideEffectList[index];
            var errorPrefix = $"sideEffects[{index}]";

            if (sideEffect.DeviceId == Guid.Empty)
            {
                errors.Add($"{errorPrefix}.deviceId is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(sideEffect.EndpointId))
            {
                errors.Add($"{errorPrefix}.endpointId is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(sideEffect.CapabilityId))
            {
                errors.Add($"{errorPrefix}.capabilityId is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(sideEffect.Operation))
            {
                errors.Add($"{errorPrefix}.operation is required.");
                continue;
            }

            if (sideEffect.DelayMs < 0)
            {
                errors.Add($"{errorPrefix}.delayMs must be >= 0.");
                continue;
            }

            if (!deviceMap.TryGetValue(sideEffect.DeviceId, out var device))
            {
                errors.Add($"{errorPrefix}: device '{sideEffect.DeviceId}' is not found in home '{homeId}'.");
                continue;
            }

            var normalizedEndpointId = sideEffect.EndpointId.Trim();
            var normalizedCapabilityId = sideEffect.CapabilityId.Trim();
            var normalizedOperation = sideEffect.Operation.Trim();

            if (targetKeys.Contains(ToTargetKey(sideEffect.DeviceId, normalizedEndpointId, normalizedCapabilityId)))
            {
                errors.Add(
                    $"{errorPrefix}: side effect '{normalizedCapabilityId}@{normalizedEndpointId}' cannot target the same capability as a scene target.");
                continue;
            }

            var capability = device.FindCapability(normalizedCapabilityId, normalizedEndpointId);
            if (capability is null)
            {
                errors.Add(
                    $"{errorPrefix}: capability '{normalizedCapabilityId}@{normalizedEndpointId}' is not found on device '{sideEffect.DeviceId}'.");
                continue;
            }

            if (!capabilityRegistry.TryGetDefinition(
                    capability.CapabilityId,
                    capability.CapabilityVersion,
                    out var definition))
            {
                errors.Add(
                    $"{errorPrefix}: capability definition '{capability.CapabilityId}@{capability.CapabilityVersion}' is not found in registry.");
                continue;
            }

            if (definition.Role == CapabilityRole.Sensor)
            {
                errors.Add(
                    $"{errorPrefix}: capability '{capability.CapabilityId}' has role 'Sensor' and cannot be used as side effect.");
                continue;
            }

            if (!definition.SupportsOperation(normalizedOperation))
            {
                errors.Add(
                    $"{errorPrefix}: operation '{normalizedOperation}' is not defined for capability '{capability.CapabilityId}'.");
                continue;
            }

            Dictionary<string, object?> normalizedParams;
            try
            {
                var normalizedPayload = capabilityCommandValidator.ValidateAndNormalize(
                    capability,
                    normalizedOperation,
                    sideEffect.Params);

                normalizedParams = ToDictionaryPayload(normalizedPayload);
            }
            catch (Exception ex)
            {
                errors.Add(
                    $"{errorPrefix}.params for operation '{normalizedOperation}' is invalid: {ex.Message}");
                continue;
            }

            validatedSideEffects.Add(new ValidatedSideEffect(
                index,
                sideEffect.DeviceId,
                normalizedEndpointId,
                capability.CapabilityId,
                normalizedOperation,
                normalizedParams,
                sideEffect.Timing,
                sideEffect.DelayMs));
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(string.Join(" | ", errors));
        }

        return validatedSideEffects
            .OrderBy(item => item.Index)
            .Select(item => new SceneSideEffectDefinition(
                item.DeviceId,
                item.EndpointId,
                item.CapabilityId,
                item.Operation,
                item.Params,
                item.Timing,
                item.DelayMs))
            .ToList();
    }

    private static void ValidateCapabilityConflicts(
        IReadOnlyCollection<ValidatedTarget> validatedTargets,
        ICollection<string> errors)
    {
        var byEndpoint = validatedTargets
            .GroupBy(
                target => (target.DeviceId, target.EndpointId),
                EndpointTargetComparer.Instance)
            .ToList();

        foreach (var group in byEndpoint)
        {
            var endpointTargets = group.OrderBy(item => item.Index).ToList();
            for (var i = 0; i < endpointTargets.Count; i++)
            {
                for (var j = i + 1; j < endpointTargets.Count; j++)
                {
                    var left = endpointTargets[i];
                    var right = endpointTargets[j];

                    var leftConflictsRight = left.Definition.ConflictsWithCapability(right.CapabilityId);
                    var rightConflictsLeft = right.Definition.ConflictsWithCapability(left.CapabilityId);
                    if (!leftConflictsRight && !rightConflictsLeft)
                        continue;

                    errors.Add(
                        $"targets[{left.Index}] capability '{left.CapabilityId}' conflicts with targets[{right.Index}] capability '{right.CapabilityId}' on endpoint '{left.EndpointId}'.");
                }
            }
        }
    }

    private static string ToTargetKey(Guid deviceId, string endpointId, string capabilityId)
    {
        return $"{deviceId:N}|{endpointId.Trim().ToLowerInvariant()}|{capabilityId.Trim().ToLowerInvariant()}";
    }

    private static Dictionary<string, object?> ToDictionaryPayload(object? payload)
    {
        return payload switch
        {
            null => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
            Dictionary<string, object?> dictionary =>
                new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase),
            IReadOnlyDictionary<string, object?> readOnlyDictionary =>
                readOnlyDictionary.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase),
            _ => throw new InvalidOperationException("Side effect params payload must be an object.")
        };
    }

    private sealed class SceneTargetKeyComparer :
        IEqualityComparer<(Guid DeviceId, string CapabilityId, string EndpointId)>
    {
        public static SceneTargetKeyComparer Instance { get; } = new();

        public bool Equals(
            (Guid DeviceId, string CapabilityId, string EndpointId) x,
            (Guid DeviceId, string CapabilityId, string EndpointId) y)
        {
            return x.DeviceId == y.DeviceId
                && string.Equals(x.CapabilityId, y.CapabilityId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.EndpointId, y.EndpointId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((Guid DeviceId, string CapabilityId, string EndpointId) obj)
        {
            return HashCode.Combine(
                obj.DeviceId,
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CapabilityId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.EndpointId));
        }
    }

    private sealed class EndpointTargetComparer : IEqualityComparer<(Guid DeviceId, string EndpointId)>
    {
        public static EndpointTargetComparer Instance { get; } = new();

        public bool Equals((Guid DeviceId, string EndpointId) x, (Guid DeviceId, string EndpointId) y)
        {
            return x.DeviceId == y.DeviceId
                && string.Equals(x.EndpointId, y.EndpointId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((Guid DeviceId, string EndpointId) obj)
        {
            return HashCode.Combine(
                obj.DeviceId,
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.EndpointId));
        }
    }
}
