using System.Text.Json;
using Core.Common;

namespace Application.ActionSets.Planning;

internal static class ActionStateHelper
{
    public static Dictionary<string, object?> NormalizeState(Dictionary<string, object?> state)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in state)
        {
            normalized[item.Key] = NormalizeValue(item.Value);
        }

        return normalized;
    }

    public static Dictionary<string, object?> NormalizeState(IReadOnlyDictionary<string, object?> state)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in state)
        {
            normalized[item.Key] = NormalizeValue(item.Value);
        }

        return normalized;
    }

    public static bool AreEquivalent(
        IReadOnlyDictionary<string, object?> currentState,
        Dictionary<string, object?> desiredState)
    {
        var current = NormalizeState(currentState);
        var desired = NormalizeState(desiredState);

        foreach (var desiredItem in desired)
        {
            if (!current.TryGetValue(desiredItem.Key, out var currentValue))
                return false;

            if (!AreValuesEquivalent(desiredItem.Value, currentValue))
                return false;
        }

        return true;
    }

    public static Dictionary<string, object?> BuildUnresolvedDiff(
        IReadOnlyDictionary<string, object?> currentState,
        Dictionary<string, object?> desiredState)
    {
        var current = NormalizeState(currentState);
        var desired = NormalizeState(desiredState);
        var diff = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var desiredItem in desired)
        {
            current.TryGetValue(desiredItem.Key, out var currentValue);

            if (AreValuesEquivalent(desiredItem.Value, currentValue))
                continue;

            diff[desiredItem.Key] = new Dictionary<string, object?>
            {
                ["current"] = currentValue,
                ["desired"] = desiredItem.Value
            };
        }

        return diff;
    }

    public static (bool Success, Dictionary<string, object?> Payload, string? Error) BuildApplyStrategyPayload(
        IReadOnlyDictionary<string, object?> desiredState,
        CapabilityApplyStrategyDefinition strategy)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in strategy.StateMapping)
        {
            if (!desiredState.TryGetValue(mapping.Key, out var desiredValue))
            {
                if (strategy.PartialUpdate)
                    continue;

                return (
                    false,
                    payload,
                    $"applyStrategy.partialUpdate=false requires state field '{mapping.Key}' to be present in state.");
            }

            payload[mapping.Value] = desiredValue;
        }

        return (true, payload, null);
    }

    public static Dictionary<string, object?> MergePayloadOptions(
        Dictionary<string, object?> payload,
        Dictionary<string, object?> options,
        out IReadOnlyList<string> collidingKeys)
    {
        var collisions = payload.Keys
            .Where(key => options.ContainsKey(key))
            .ToList();
        collidingKeys = collisions;

        var merged = new Dictionary<string, object?>(payload, StringComparer.OrdinalIgnoreCase);
        foreach (var option in options)
        {
            merged[option.Key] = option.Value;
        }

        return merged;
    }

    private static bool AreValuesEquivalent(object? left, object? right)
    {
        var normalizedLeft = NormalizeValue(left);
        var normalizedRight = NormalizeValue(right);

        if (normalizedLeft is null || normalizedRight is null)
            return normalizedLeft is null && normalizedRight is null;

        if (TryConvertNumber(normalizedLeft, out var leftNumber)
            && TryConvertNumber(normalizedRight, out var rightNumber))
        {
            return leftNumber == rightNumber;
        }

        if (normalizedLeft is string leftText && normalizedRight is string rightText)
            return string.Equals(leftText, rightText, StringComparison.OrdinalIgnoreCase);

        if (normalizedLeft is bool leftBool && normalizedRight is bool rightBool)
            return leftBool == rightBool;

        if (normalizedLeft is IDictionary<string, object?> leftMap
            && normalizedRight is IDictionary<string, object?> rightMap)
        {
            if (leftMap.Count != rightMap.Count)
                return false;

            foreach (var item in leftMap)
            {
                if (!rightMap.TryGetValue(item.Key, out var rightMapValue))
                    return false;

                if (!AreValuesEquivalent(item.Value, rightMapValue))
                    return false;
            }

            return true;
        }

        if (normalizedLeft is IEnumerable<object?> leftList
            && normalizedRight is IEnumerable<object?> rightList)
        {
            var leftArray = leftList.ToArray();
            var rightArray = rightList.ToArray();
            if (leftArray.Length != rightArray.Length)
                return false;

            for (var index = 0; index < leftArray.Length; index++)
            {
                if (!AreValuesEquivalent(leftArray[index], rightArray[index]))
                    return false;
            }

            return true;
        }

        return Equals(normalizedLeft, normalizedRight);
    }

    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            JsonElement element => JsonPayloadHelper.ConvertJsonElement(element),
            IReadOnlyDictionary<string, object?> readOnlyMap => NormalizeState(readOnlyMap),
            IDictionary<string, object?> map => NormalizeState(new Dictionary<string, object?>(map)),
            IEnumerable<object?> list => list.Select(NormalizeValue).ToList(),
            _ => value
        };
    }

    private static bool TryConvertNumber(object? value, out decimal number)
    {
        switch (value)
        {
            case byte v:
                number = v;
                return true;
            case sbyte v:
                number = v;
                return true;
            case short v:
                number = v;
                return true;
            case ushort v:
                number = v;
                return true;
            case int v:
                number = v;
                return true;
            case uint v:
                number = v;
                return true;
            case long v:
                number = v;
                return true;
            case ulong v:
                number = v;
                return true;
            case float v:
                number = (decimal)v;
                return true;
            case double v:
                number = (decimal)v;
                return true;
            case decimal v:
                number = v;
                return true;
            default:
                number = default;
                return false;
        }
    }
}
