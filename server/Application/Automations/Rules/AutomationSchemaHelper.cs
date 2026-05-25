using System.Text.Json;
using Core.Common;

namespace Application.Automations.Rules;

internal sealed record AutomationSchemaField(
    string Path,
    string Type,
    IReadOnlyList<string> EnumValues);

internal static class AutomationSchemaHelper
{
    public static IReadOnlyList<AutomationSchemaField> ExtractFields(string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            return [];

        using var document = JsonDocument.Parse(schemaJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return [];

        var fields = new List<AutomationSchemaField>();
        CollectFields(document.RootElement, string.Empty, fields);
        return fields;
    }

    public static bool TryGetValueByPath(
        IReadOnlyDictionary<string, object?> state,
        string fieldPath,
        out object? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(fieldPath))
            return false;

        object? current = NormalizeValue(state);
        foreach (var segment in fieldPath
            .Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            current = NormalizeValue(current);

            if (current is IReadOnlyDictionary<string, object?> readOnlyMap)
            {
                if (!TryGetMapValue(readOnlyMap, segment, out current))
                    return false;

                continue;
            }

            if (current is IDictionary<string, object?> map)
            {
                if (!TryGetMapValue(map, segment, out current))
                    return false;

                continue;
            }

            return false;
        }

        value = NormalizeValue(current);
        return true;
    }

    public static object? NormalizeValue(object? value)
    {
        return value switch
        {
            JsonElement element => JsonPayloadHelper.ConvertJsonElement(element),
            IReadOnlyDictionary<string, object?> readOnlyMap => readOnlyMap
                .ToDictionary(item => item.Key, item => NormalizeValue(item.Value), StringComparer.OrdinalIgnoreCase),
            IDictionary<string, object?> map => map
                .ToDictionary(item => item.Key, item => NormalizeValue(item.Value), StringComparer.OrdinalIgnoreCase),
            IEnumerable<object?> list => list.Select(NormalizeValue).ToList(),
            _ => value
        };
    }

    public static Dictionary<string, object?> NormalizeObject(Dictionary<string, object?> value)
    {
        return value
            .ToDictionary(item => item.Key, item => NormalizeValue(item.Value), StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryConvertNumber(object? value, out double number)
    {
        value = NormalizeValue(value);
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
                number = v;
                return true;
            case double v:
                number = v;
                return true;
            case decimal v:
                number = (double)v;
                return true;
            default:
                number = default;
                return false;
        }
    }

    private static void CollectFields(JsonElement schema, string currentPath, ICollection<AutomationSchemaField> fields)
    {
        var type = GetPrimaryType(schema);
        if ((type == "object" || type is null)
            && schema.TryGetProperty("properties", out var propertiesNode)
            && propertiesNode.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in propertiesNode.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                    continue;

                var path = string.IsNullOrWhiteSpace(currentPath)
                    ? property.Name
                    : $"{currentPath}.{property.Name}";
                CollectFields(property.Value, path, fields);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(currentPath))
            return;

        fields.Add(new AutomationSchemaField(
            currentPath,
            type ?? "unsupported",
            GetEnumValues(schema)));
    }

    private static string? GetPrimaryType(JsonElement schema)
    {
        if (schema.TryGetProperty("type", out var typeNode))
        {
            if (typeNode.ValueKind == JsonValueKind.String)
                return typeNode.GetString()?.Trim().ToLowerInvariant();

            if (typeNode.ValueKind == JsonValueKind.Array)
            {
                return typeNode.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString()?.Trim().ToLowerInvariant())
                    .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item) && item != "null");
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetEnumValues(JsonElement schema)
    {
        if (!schema.TryGetProperty("enum", out var enumNode)
            || enumNode.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return enumNode.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToList();
    }

    private static bool TryGetMapValue(
        IReadOnlyDictionary<string, object?> map,
        string segment,
        out object? value)
    {
        foreach (var item in map)
        {
            if (item.Key.Equals(segment, StringComparison.OrdinalIgnoreCase))
            {
                value = item.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetMapValue(
        IDictionary<string, object?> map,
        string segment,
        out object? value)
    {
        foreach (var item in map)
        {
            if (item.Key.Equals(segment, StringComparison.OrdinalIgnoreCase))
            {
                value = item.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}
