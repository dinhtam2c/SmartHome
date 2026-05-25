namespace Domain.Common;

public static class StructuredValue
{
    public static Dictionary<string, object?> SnapshotDictionary(
        IReadOnlyDictionary<string, object?> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.ToDictionary(
            item => item.Key,
            item => Snapshot(item.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    public static object? Snapshot(object? value)
    {
        return value switch
        {
            IReadOnlyDictionary<string, object?> dictionary => SnapshotDictionary(dictionary),
            IDictionary<string, object?> dictionary => SnapshotDictionary(
                new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase)),
            IEnumerable<object?> items => items.Select(Snapshot).ToList(),
            _ => value
        };
    }

    public static bool AreEqual(
        object? left,
        object? right,
        StringComparison stringComparison = StringComparison.Ordinal)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        if (TryConvertNumber(left, out var leftNumber)
            && TryConvertNumber(right, out var rightNumber))
        {
            return leftNumber == rightNumber;
        }

        if (left is string leftText && right is string rightText)
            return leftText.Equals(rightText, stringComparison);

        if (left is IReadOnlyDictionary<string, object?> leftDictionary
            && right is IReadOnlyDictionary<string, object?> rightDictionary)
        {
            return DictionariesEqual(leftDictionary, rightDictionary, stringComparison);
        }

        if (left is IEnumerable<object?> leftItems && right is IEnumerable<object?> rightItems)
            return SequencesEqual(leftItems, rightItems, stringComparison);

        return Equals(left, right);
    }

    private static bool DictionariesEqual(
        IReadOnlyDictionary<string, object?> left,
        IReadOnlyDictionary<string, object?> right,
        StringComparison stringComparison)
    {
        if (left.Count != right.Count)
            return false;

        var rightByKey = new Dictionary<string, object?>(right, StringComparer.OrdinalIgnoreCase);
        foreach (var item in left)
        {
            if (!rightByKey.TryGetValue(item.Key, out var rightValue)
                || !AreEqual(item.Value, rightValue, stringComparison))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SequencesEqual(
        IEnumerable<object?> left,
        IEnumerable<object?> right,
        StringComparison stringComparison)
    {
        using var leftEnumerator = left.GetEnumerator();
        using var rightEnumerator = right.GetEnumerator();

        while (leftEnumerator.MoveNext())
        {
            if (!rightEnumerator.MoveNext()
                || !AreEqual(leftEnumerator.Current, rightEnumerator.Current, stringComparison))
            {
                return false;
            }
        }

        return !rightEnumerator.MoveNext();
    }

    private static bool TryConvertNumber(object value, out decimal number)
    {
        try
        {
            switch (value)
            {
                case byte or sbyte or short or ushort or int or uint or long or ulong or decimal:
                    number = Convert.ToDecimal(value);
                    return true;
                case float floatValue when float.IsFinite(floatValue):
                    number = Convert.ToDecimal(floatValue);
                    return true;
                case double doubleValue when double.IsFinite(doubleValue):
                    number = Convert.ToDecimal(doubleValue);
                    return true;
                default:
                    number = default;
                    return false;
            }
        }
        catch (OverflowException)
        {
            number = default;
            return false;
        }
    }
}
