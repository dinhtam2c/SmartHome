using Domain.Models.Automations;

namespace Application.BusinessServices.Automations.Rules;

internal static class AutomationConditionEvaluator
{
    public static bool Evaluate(
        AutomationCondition condition,
        IReadOnlyDictionary<string, object?> state)
    {
        if (!AutomationSchemaHelper.TryGetValueByPath(state, condition.FieldPath, out var actualValue))
            return false;

        var compareValue = AutomationSchemaHelper.NormalizeValue(condition.CompareValue);

        return condition.Operator switch
        {
            AutomationConditionOperator.Equals => AreEquivalent(actualValue, compareValue),
            AutomationConditionOperator.NotEquals => !AreEquivalent(actualValue, compareValue),
            AutomationConditionOperator.GreaterThan => CompareNumbers(actualValue, compareValue, static (left, right) => left > right),
            AutomationConditionOperator.GreaterThanOrEqual => CompareNumbers(actualValue, compareValue, static (left, right) => left >= right),
            AutomationConditionOperator.LessThan => CompareNumbers(actualValue, compareValue, static (left, right) => left < right),
            AutomationConditionOperator.LessThanOrEqual => CompareNumbers(actualValue, compareValue, static (left, right) => left <= right),
            AutomationConditionOperator.Between => IsBetween(actualValue, compareValue),
            _ => false
        };
    }

    public static bool EvaluateRule(
        AutomationRule rule,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stateByConditionKey,
        long now)
    {
        var orderedConditions = rule.Conditions.OrderBy(condition => condition.Order).ToList();
        if (orderedConditions.Count == 0)
            return false;

        var localNow = DateTimeOffset.FromUnixTimeSeconds(now).ToLocalTime();
        if (!EvaluateTimeWindow(rule, localNow))
            return false;

        var deviceConditionResults = orderedConditions
            .Select(condition =>
            {
                var key = BuildConditionKey(condition.DeviceId, condition.EndpointId, condition.CapabilityId);
                return stateByConditionKey.TryGetValue(key, out var state)
                    && Evaluate(condition, state);
            })
            .ToList();

        if (deviceConditionResults.Count == 0)
            return false;

        return rule.ConditionLogic == AutomationConditionLogic.All
            ? deviceConditionResults.All(result => result)
            : deviceConditionResults.Any(result => result);
    }

    public static string BuildConditionKey(Guid? deviceId, string? endpointId, string? capabilityId)
    {
        return $"{(deviceId ?? Guid.Empty).ToString("N")}|{endpointId?.Trim().ToLowerInvariant()}|{capabilityId?.Trim().ToLowerInvariant()}";
    }

    private static bool EvaluateTimeWindow(AutomationRule rule, DateTimeOffset localNow)
    {
        if (!rule.TimeWindowEnabled)
            return true;

        if (!rule.TimeWindowStartMinute.HasValue || !rule.TimeWindowEndMinute.HasValue)
            return false;

        var nowMinute = localNow.Hour * 60 + localNow.Minute;
        var startMinute = rule.TimeWindowStartMinute.Value;
        var endMinute = rule.TimeWindowEndMinute.Value;
        var crossesMidnight = startMinute > endMinute;
        var inWindow = crossesMidnight
            ? nowMinute >= startMinute || nowMinute <= endMinute
            : nowMinute >= startMinute && nowMinute <= endMinute;

        if (!inWindow)
            return false;

        var dayForWindow = crossesMidnight && nowMinute <= endMinute
            ? localNow.AddDays(-1).DayOfWeek
            : localNow.DayOfWeek;

        var daysMask = rule.TimeWindowDaysOfWeekMask == 0
            ? 0b111_1111
            : rule.TimeWindowDaysOfWeekMask;

        return (daysMask & (1 << (int)dayForWindow)) != 0;
    }

    private static bool CompareNumbers(
        object? actualValue,
        object? compareValue,
        Func<double, double, bool> predicate)
    {
        return AutomationSchemaHelper.TryConvertNumber(actualValue, out var actualNumber)
            && AutomationSchemaHelper.TryConvertNumber(compareValue, out var compareNumber)
            && predicate(actualNumber, compareNumber);
    }

    private static bool IsBetween(object? actualValue, object? compareValue)
    {
        if (!AutomationSchemaHelper.TryConvertNumber(actualValue, out var actualNumber))
            return false;

        if (compareValue is not IReadOnlyDictionary<string, object?> readOnlyMap)
            return false;

        return readOnlyMap.TryGetValue("min", out var minValue)
            && readOnlyMap.TryGetValue("max", out var maxValue)
            && AutomationSchemaHelper.TryConvertNumber(minValue, out var min)
            && AutomationSchemaHelper.TryConvertNumber(maxValue, out var max)
            && min <= max
            && actualNumber >= min
            && actualNumber <= max;
    }

    private static bool AreEquivalent(object? actualValue, object? compareValue)
    {
        actualValue = AutomationSchemaHelper.NormalizeValue(actualValue);
        compareValue = AutomationSchemaHelper.NormalizeValue(compareValue);

        if (actualValue is null || compareValue is null)
            return actualValue is null && compareValue is null;

        if (AutomationSchemaHelper.TryConvertNumber(actualValue, out var actualNumber)
            && AutomationSchemaHelper.TryConvertNumber(compareValue, out var compareNumber))
        {
            return actualNumber.Equals(compareNumber);
        }

        if (actualValue is string actualText && compareValue is string compareText)
            return actualText.Equals(compareText, StringComparison.OrdinalIgnoreCase);

        return Equals(actualValue, compareValue);
    }
}
