using System.Text.Json;

namespace Application.Common.Json;

public static class JsonHelper
{
    public static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var value) => value,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.Object => JsonObjectToDictionary(element),
            JsonValueKind.Array => JsonArrayToList(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static Dictionary<string, object?> JsonObjectToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = JsonElementToObject(property.Value);
        }

        return result;
    }

    private static List<object?> JsonArrayToList(JsonElement element)
    {
        var result = new List<object?>();

        foreach (var item in element.EnumerateArray())
        {
            result.Add(JsonElementToObject(item));
        }

        return result;
    }

    public static string? SerializePayload(object? value)
    {
        if (value is null)
            return null;

        return JsonSerializer.Serialize(value);
    }
}
