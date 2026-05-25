using System.Text.Json;
using Core.Common;

namespace Application.Queries.Automations;

internal static class AutomationPayloadMapper
{
    public static Dictionary<string, object?> ParseObject(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            dictionary[property.Name] = JsonPayloadHelper.ConvertJsonElement(property.Value);
        }

        return dictionary;
    }

    public static object? ParseValue(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        using var document = JsonDocument.Parse(payload);
        return JsonPayloadHelper.ConvertJsonElement(document.RootElement);
    }
}
