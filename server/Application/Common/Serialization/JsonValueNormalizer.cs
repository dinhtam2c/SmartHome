using System.Text.Json;

namespace Application.Common.Serialization;

public static class JsonValueNormalizer
{
    public static object? Normalize(object? value)
    {
        return value switch
        {
            JsonElement element => NormalizeElement(element),
            IReadOnlyDictionary<string, object?> readOnlyDictionary =>
                NormalizeObject(readOnlyDictionary),
            IDictionary<string, object?> dictionary =>
                NormalizeObject(dictionary),
            IEnumerable<object?> values => values.Select(Normalize).ToList(),
            _ => value
        };
    }

    public static Dictionary<string, object?> NormalizeObject(
        IEnumerable<KeyValuePair<string, object?>> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.ToDictionary(
            item => item.Key,
            item => Normalize(item.Value),
            StringComparer.Ordinal);
    }

    private static object? NormalizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => NormalizeElement(property.Value),
                StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(NormalizeElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) =>
                integer is >= int.MinValue and <= int.MaxValue ? (int)integer : integer,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}
