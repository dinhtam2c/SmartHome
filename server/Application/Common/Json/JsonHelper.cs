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
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    public static string? SerializePayload(object? value)
    {
        if (value is null)
            return null;

        return JsonSerializer.Serialize(value);
    }
}
