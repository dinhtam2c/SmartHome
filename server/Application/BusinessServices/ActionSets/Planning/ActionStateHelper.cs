using Application.Common.Serialization;
using Domain.Models.Capabilities;
using Domain.Common;

namespace Application.BusinessServices.ActionSets.Planning;

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

            if (!StructuredValue.AreEqual(
                    desiredItem.Value,
                    currentValue,
                    StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
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

    private static object? NormalizeValue(object? value)
    {
        var normalized = JsonValueNormalizer.Normalize(value);
        return normalized switch
        {
            IReadOnlyDictionary<string, object?> map => NormalizeState(map),
            IEnumerable<object?> values => values.Select(NormalizeValue).ToList(),
            _ => normalized
        };
    }

}
