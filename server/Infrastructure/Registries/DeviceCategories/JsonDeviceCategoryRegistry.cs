using System.Text.Json;
using Application.Common.DeviceCategories;
using Core.Domain.DeviceCategories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Registries.DeviceCategories;

public sealed class DeviceCategoryRegistryFileOptions
{
    public string FilePath { get; set; } = "device-category-registry/index.json";
}

public sealed class JsonDeviceCategoryRegistry : IDeviceCategoryRegistry
{
    private readonly Dictionary<string, DeviceCategoryDefinition> _definitions;

    public JsonDeviceCategoryRegistry(
        IOptions<DeviceCategoryRegistryFileOptions> options,
        ILogger<JsonDeviceCategoryRegistry> logger)
    {
        var filePath = ResolveFilePath(options.Value.FilePath);
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Device category registry file was not found: {filePath}");
        }

        _definitions = LoadDefinitions(filePath);

        logger.LogInformation(
            "Loaded device category registry from {Path} with {Count} entries",
            filePath,
            _definitions.Count);
    }

    public bool TryGetDefinition(string categoryId, out DeviceCategoryDefinition definition)
    {
        var normalized = DeviceCategoryIds.Normalize(categoryId);
        return _definitions.TryGetValue(normalized, out definition!);
    }

    public DeviceCategoryDefinition GetRequiredDefinition(string categoryId)
    {
        if (TryGetDefinition(categoryId, out var definition))
            return definition;

        throw new InvalidOperationException(
            $"Device category definition was not found in registry: {categoryId}");
    }

    public IReadOnlyCollection<DeviceCategoryDefinition> GetAll()
    {
        return _definitions.Values.ToList();
    }

    private static Dictionary<string, DeviceCategoryDefinition> LoadDefinitions(string filePath)
    {
        using var document = ParseJsonDocument(filePath);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"Device category registry root must be a JSON array: {filePath}");
        }

        var result = new Dictionary<string, DeviceCategoryDefinition>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var entry in root.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"Device category registry entry at position {index} must be an object: {filePath}");
            }

            var definition = new DeviceCategoryDefinition(
                Id: DeviceCategoryIds.Normalize(GetRequiredString(entry, "id", filePath)),
                DefaultName: GetRequiredString(entry, "defaultName", filePath),
                IconKey: GetRequiredString(entry, "iconKey", filePath),
                Color: GetRequiredString(entry, "color", filePath),
                Order: GetRequiredInt(entry, "order", filePath));

            if (!result.TryAdd(definition.Id, definition))
            {
                throw new InvalidOperationException(
                    $"Device category registry has duplicate entry: {definition.Id}. Source: {filePath}");
            }

            index++;
        }

        if (!result.ContainsKey(DeviceCategoryIds.Other))
        {
            throw new InvalidOperationException(
                $"Device category registry must contain '{DeviceCategoryIds.Other}': {filePath}");
        }

        return result;
    }

    private static JsonDocument ParseJsonDocument(string filePath)
    {
        try
        {
            return JsonDocument.Parse(File.ReadAllText(filePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidOperationException(
                $"Device category registry file is not valid JSON: {filePath}",
                ex);
        }
    }

    private static string GetRequiredString(JsonElement entry, string propertyName, string sourcePath)
    {
        if (!entry.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException(
                $"Device category registry entry requires non-empty '{propertyName}': {sourcePath}");
        }

        return property.GetString()!.Trim();
    }

    private static int GetRequiredInt(JsonElement entry, string propertyName, string sourcePath)
    {
        if (!entry.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out var value))
        {
            throw new InvalidOperationException(
                $"Device category registry entry requires integer '{propertyName}': {sourcePath}");
        }

        return value;
    }

    private static string ResolveFilePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}
