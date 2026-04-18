using System.Text.Json;
using Core.Common;

namespace Application.Commands.Scenes;

internal static class SceneStateHelper
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

    public static object? TryExtractScalarValue(Dictionary<string, object?> desiredState)
    {
        if (desiredState.TryGetValue("value", out var value))
            return value;

        if (desiredState.Count == 1)
            return desiredState.Values.FirstOrDefault();

        return null;
    }

    public static bool TryExtractBooleanValue(object? value, out bool booleanValue)
    {
        if (value is bool boolValue)
        {
            booleanValue = boolValue;
            return true;
        }

        if (value is string textValue && bool.TryParse(textValue, out var parsed))
        {
            booleanValue = parsed;
            return true;
        }

        booleanValue = default;
        return false;
    }

    public static bool TryExtractCurrentBoolean(
        IReadOnlyDictionary<string, object?> currentState,
        Dictionary<string, object?> desiredState,
        out bool booleanValue)
    {
        if (currentState.TryGetValue("value", out var value)
            && TryExtractBooleanValue(value, out var currentBoolean))
        {
            booleanValue = currentBoolean;
            return true;
        }

        if (desiredState.Count == 1)
        {
            var key = desiredState.Keys.First();
            if (currentState.TryGetValue(key, out var valueByKey)
                && TryExtractBooleanValue(valueByKey, out var keyedBoolean))
            {
                booleanValue = keyedBoolean;
                return true;
            }
        }

        if (currentState.Count == 1)
        {
            var onlyValue = currentState.Values.FirstOrDefault();
            if (TryExtractBooleanValue(onlyValue, out var singleBoolean))
            {
                booleanValue = singleBoolean;
                return true;
            }
        }

        booleanValue = default;
        return false;
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
