using System.Text.Json;
using Application.Common.Serialization;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Persistence.Serialization;

internal static class JsonColumnSerializer
{
    public static string Serialize(object? value)
    {
        return JsonSerializer.Serialize(Canonicalize(value));
    }

    public static Dictionary<string, object?> DeserializeDictionary(string value)
    {
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(value) ?? [];
        return JsonValueNormalizer.NormalizeObject(dictionary);
    }

    public static object? DeserializeValue(string value)
    {
        using var document = JsonDocument.Parse(value);
        return JsonValueNormalizer.Normalize(document.RootElement);
    }

    public static IReadOnlyList<T> DeserializeList<T>(string value)
    {
        return JsonSerializer.Deserialize<List<T>>(value) ?? [];
    }

    public static ValueComparer<IReadOnlyDictionary<string, object?>> CreateDictionaryComparer()
    {
        return new ValueComparer<IReadOnlyDictionary<string, object?>>(
            (left, right) => SerializedValuesEqual(left, right),
            value => GetSerializedHashCode(value),
            value => DeserializeDictionary(Serialize(value)));
    }

    public static ValueComparer<object?> CreateValueComparer()
    {
        return new ValueComparer<object?>(
            (left, right) => SerializedValuesEqual(left, right),
            value => GetSerializedHashCode(value),
            value => SnapshotValue(value));
    }

    public static ValueComparer<IReadOnlyList<T>> CreateListComparer<T>()
    {
        return new ValueComparer<IReadOnlyList<T>>(
            (left, right) => ListsEqual(left, right),
            value => GetListHashCode(value),
            value => SnapshotList(value));
    }

    private static bool SerializedValuesEqual(object? left, object? right)
    {
        return Serialize(left) == Serialize(right);
    }

    private static int GetSerializedHashCode(object? value)
    {
        return StringComparer.Ordinal.GetHashCode(Serialize(value));
    }

    private static object? SnapshotValue(object? value)
    {
        return value is null ? null : DeserializeValue(Serialize(value));
    }

    private static bool ListsEqual<T>(IReadOnlyList<T>? left, IReadOnlyList<T>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        return left is not null && right is not null && left.SequenceEqual(right);
    }

    private static int GetListHashCode<T>(IReadOnlyList<T>? value)
    {
        return value?.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)) ?? 0;
    }

    private static IReadOnlyList<T> SnapshotList<T>(IReadOnlyList<T>? value)
    {
        return value?.ToList() ?? [];
    }

    private static object? Canonicalize(object? value)
    {
        var normalized = JsonValueNormalizer.Normalize(value);
        return normalized switch
        {
            IReadOnlyDictionary<string, object?> dictionary => dictionary
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    item => item.Key,
                    item => Canonicalize(item.Value),
                    StringComparer.OrdinalIgnoreCase),
            IEnumerable<object?> items => items.Select(Canonicalize).ToList(),
            _ => normalized
        };
    }
}
