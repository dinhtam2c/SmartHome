using System.Text.Json;

namespace Core.Common;

public static class JsonPayloadHelper
{
    public static string SerializeDictionary(
        Dictionary<string, object?> value,
        string payloadName,
        bool requireNonEmpty = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (requireNonEmpty && value.Count == 0)
            throw new InvalidOperationException($"{payloadName} must contain at least one field.");

        return JsonSerializer.Serialize(value);
    }

    public static Dictionary<string, object?> DeserializeDictionary(
        string? payload,
        string payloadName,
        bool throwOnNonObject = true)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            if (throwOnNonObject)
                throw new InvalidOperationException($"{payloadName} must be a JSON object.");

            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        return document.RootElement
            .EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => ConvertJsonElement(property.Value),
                StringComparer.OrdinalIgnoreCase);
    }

    public static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => ConvertJsonElement(property.Value),
                    StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
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
