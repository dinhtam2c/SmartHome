using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Automations;
using Core.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Application.Automations.Rules;

internal static class AutomationValidationHelper
{
    private const int MinutesPerDay = 24 * 60;
    private const int AllDaysOfWeekMask = 0b111_1111;

    public static async Task<List<AutomationConditionDefinition>> ValidateAndBuildConditionDefinitions(
        Guid homeId,
        IEnumerable<AutomationConditionModel>? conditions,
        IAppReadDbContext context,
        ICapabilityRegistry capabilityRegistry,
        CancellationToken cancellationToken)
    {
        var conditionList = conditions?.ToList() ?? [];
        if (conditionList.Count == 0)
            throw new DomainValidationException("Automation rule must contain at least one device-state condition.");

        var devices = await LoadDevices(
            homeId,
            conditionList
                .Select(condition => condition.DeviceId ?? Guid.Empty),
            context,
            cancellationToken);
        var errors = new List<string>();
        var definitions = new List<AutomationConditionDefinition>(conditionList.Count);

        for (var index = 0; index < conditionList.Count; index++)
        {
            var condition = conditionList[index];
            var prefix = $"conditions[{index}]";

            if (!TryValidateDeviceCapability(
                    homeId,
                    condition.DeviceId,
                    condition.EndpointId,
                    condition.CapabilityId,
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

            if (definition.Role is not (CapabilityRole.Sensor or CapabilityRole.Control))
            {
                errors.Add($"{prefix}: capability '{capability.CapabilityId}' cannot be used as automation condition because role is '{definition.Role}'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(condition.FieldPath))
            {
                errors.Add($"{prefix}.fieldPath is required.");
                continue;
            }

            var fields = AutomationSchemaHelper.ExtractFields(definition.StateSchema);
            var field = fields.FirstOrDefault(item =>
                item.Path.Equals(condition.FieldPath.Trim(), StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                errors.Add($"{prefix}.fieldPath '{condition.FieldPath}' is not a supported leaf field for capability '{capability.CapabilityId}'.");
                continue;
            }

            if (!condition.Operator.HasValue)
            {
                errors.Add($"{prefix}.operator is required.");
                continue;
            }

            var op = condition.Operator.Value;
            if (!IsOperatorSupported(field, op))
            {
                errors.Add($"{prefix}.operator '{condition.Operator}' is not supported for field '{field.Path}' type '{field.Type}'.");
                continue;
            }

            var compareValue = AutomationSchemaHelper.NormalizeValue(condition.CompareValue);
            if (!IsCompareValueValid(field, op, compareValue))
            {
                errors.Add($"{prefix}.compareValue is invalid for operator '{condition.Operator}' and field '{field.Path}'.");
                continue;
            }

            definitions.Add(AutomationConditionDefinition.DeviceState(
                device.Id,
                endpointId,
                capability.CapabilityId,
                condition.FieldPath.Trim(),
                op,
                compareValue));
        }

        if (errors.Count > 0)
            throw new DomainValidationException(string.Join(" | ", errors));

        return definitions;
    }

    public static AutomationTimeWindowDefinition? ValidateAndBuildTimeWindowDefinition(
        AutomationTimeWindowModel? timeWindow)
    {
        if (timeWindow is null || !timeWindow.Enabled)
            return null;

        var errors = new List<string>();
        var definition = ValidateTimeWindow(timeWindow, "timeWindow", errors);

        if (errors.Count > 0)
            throw new DomainValidationException(string.Join(" | ", errors));

        return definition;
    }

    private static async Task<Dictionary<Guid, Device>> LoadDevices(
        Guid homeId,
        IEnumerable<Guid> deviceIds,
        IAppReadDbContext context,
        CancellationToken cancellationToken)
    {
        var ids = deviceIds
            .Where(deviceId => deviceId != Guid.Empty)
            .Distinct()
            .ToList();

        return await context.Devices
            .AsNoTracking()
            .Include(device => device.Endpoints)
            .ThenInclude(endpoint => endpoint.Capabilities)
            .AsSplitQuery()
            .Where(device => device.HomeId == homeId && ids.Contains(device.Id))
            .ToDictionaryAsync(device => device.Id, cancellationToken);
    }

    private static bool TryValidateDeviceCapability(
        Guid homeId,
        Guid? deviceId,
        string? endpointId,
        string? capabilityId,
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

        if (!deviceId.HasValue || deviceId.Value == Guid.Empty)
        {
            errors.Add($"{prefix}.deviceId is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(endpointId))
        {
            errors.Add($"{prefix}.endpointId is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(capabilityId))
        {
            errors.Add($"{prefix}.capabilityId is required.");
            return false;
        }

        if (!devices.TryGetValue(deviceId.Value, out device!))
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

    private static AutomationTimeWindowDefinition? ValidateTimeWindow(
        AutomationTimeWindowModel? timeWindow,
        string prefix,
        ICollection<string> errors)
    {
        if (timeWindow is null || !timeWindow.Enabled)
            return null;

        if (!TryParseTimeMinute(timeWindow.StartTime, out var startMinute))
        {
            errors.Add($"{prefix}.startTime must be HH:mm.");
            return null;
        }

        if (!TryParseTimeMinute(timeWindow.EndTime, out var endMinute))
        {
            errors.Add($"{prefix}.endTime must be HH:mm.");
            return null;
        }

        if (!TryBuildDaysOfWeekMask(timeWindow.DaysOfWeek, out var daysOfWeekMask))
        {
            errors.Add($"{prefix}.daysOfWeek contains an invalid day.");
            return null;
        }

        return new AutomationTimeWindowDefinition(
            startMinute,
            endMinute,
            daysOfWeekMask);
    }

    private static bool TryParseTimeMinute(string? value, out int minute)
    {
        minute = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Trim().Split(':');
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var hour)
            || !int.TryParse(parts[1], out var minutes)
            || hour < 0
            || hour > 23
            || minutes < 0
            || minutes > 59)
        {
            return false;
        }

        minute = hour * 60 + minutes;
        return minute >= 0 && minute < MinutesPerDay;
    }

    private static bool TryBuildDaysOfWeekMask(IReadOnlyList<DayOfWeek>? daysOfWeek, out int mask)
    {
        if (daysOfWeek is null || daysOfWeek.Count == 0)
        {
            mask = AllDaysOfWeekMask;
            return true;
        }

        mask = 0;
        foreach (var day in daysOfWeek.Distinct())
        {
            if (!Enum.IsDefined(day))
                return false;

            mask |= 1 << (int)day;
        }

        return mask != 0;
    }

    private static bool IsOperatorSupported(AutomationSchemaField field, AutomationConditionOperator op)
    {
        if (field.EnumValues.Count > 0 || field.Type == "string" || field.Type == "boolean")
            return op is AutomationConditionOperator.Equals or AutomationConditionOperator.NotEquals;

        if (field.Type is "integer" or "number")
            return true;

        return false;
    }

    private static bool IsCompareValueValid(
        AutomationSchemaField field,
        AutomationConditionOperator op,
        object? compareValue)
    {
        if (op == AutomationConditionOperator.Between)
        {
            return compareValue is IReadOnlyDictionary<string, object?> map
                && map.TryGetValue("min", out var minValue)
                && map.TryGetValue("max", out var maxValue)
                && AutomationSchemaHelper.TryConvertNumber(minValue, out var min)
                && AutomationSchemaHelper.TryConvertNumber(maxValue, out var max)
                && min <= max;
        }

        if (field.Type is "integer" or "number")
            return AutomationSchemaHelper.TryConvertNumber(compareValue, out _);

        if (field.Type == "boolean")
            return compareValue is bool;

        if (field.EnumValues.Count > 0)
        {
            return compareValue is string enumText
                && field.EnumValues.Any(item => item.Equals(enumText, StringComparison.OrdinalIgnoreCase));
        }

        return field.Type == "string" && compareValue is string;
    }
}
