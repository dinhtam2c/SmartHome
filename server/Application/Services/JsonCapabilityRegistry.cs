using System.Text.Json;
using Core.Domain.Devices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public sealed class JsonCapabilityRegistry : ICapabilityRegistry
{
    private readonly Dictionary<string, CapabilityDefinition> _definitions;

    public JsonCapabilityRegistry(
        IOptions<CapabilityRegistryFileOptions> options,
        ILogger<JsonCapabilityRegistry> logger)
    {
        var filePath = ResolveFilePath(options.Value.FilePath);
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Capability registry file was not found: {filePath}");
        }

        _definitions = LoadDefinitions(filePath);

        logger.LogInformation(
            "Loaded capability registry from {Path} with {Count} entries",
            filePath,
            _definitions.Count);
    }

    public bool TryGetDefinition(string capabilityId, int version, out CapabilityDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(capabilityId) || version <= 0)
        {
            definition = default!;
            return false;
        }

        return _definitions.TryGetValue(ToKey(capabilityId, version), out definition!);
    }

    public CapabilityDefinition GetRequiredDefinition(string capabilityId, int version)
    {
        if (TryGetDefinition(capabilityId, version, out var definition))
            return definition;

        throw new InvalidOperationException(
            $"Capability definition was not found in registry: {capabilityId}@{version}");
    }

    public IReadOnlyCollection<CapabilityDefinition> GetAll()
    {
        return _definitions.Values.ToList();
    }

    private static Dictionary<string, CapabilityDefinition> LoadDefinitions(string filePath)
    {
        var json = File.ReadAllText(filePath);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Capability registry file is not valid JSON: {filePath}",
                ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Capability registry root must be a JSON array.");
            }

            var result = new Dictionary<string, CapabilityDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in root.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException("Each capability registry entry must be an object.");
                }

                var capabilityId = GetRequiredString(entry, "id");
                var version = GetRequiredInt(entry, "version");
                var role = GetRequiredEnum<CapabilityRole>(entry, "role");

                var stateSchema = ResolveStateSchema(entry, role, capabilityId, version);
                var operations = ParseOperations(entry, role, capabilityId, version);
                var metadata = ResolveMetadata(entry, capabilityId, version);
                var conflictsWith = ParseConflictsWith(entry, capabilityId, version);
                var prerequisite = ParsePrerequisite(entry, capabilityId, version);
                var applyStrategy = ParseApplyStrategy(
                    entry,
                    role,
                    stateSchema,
                    operations,
                    capabilityId,
                    version);

                var definition = new CapabilityDefinition(
                    id: capabilityId,
                    version: version,
                    role: role,
                    stateSchema: stateSchema,
                    metadata: metadata,
                    operations: operations,
                    conflictsWith: conflictsWith,
                    prerequisite: prerequisite,
                    applyStrategy: applyStrategy);

                var key = ToKey(definition.Id, definition.Version);
                if (!result.TryAdd(key, definition))
                {
                    throw new InvalidOperationException(
                        $"Capability registry has duplicate entry: {definition.Id}@{definition.Version}");
                }
            }

            ValidateCrossReferences(result);
            return result;
        }
    }

    private static void ValidateCrossReferences(
        IReadOnlyDictionary<string, CapabilityDefinition> definitions)
    {
        foreach (var definition in definitions.Values)
        {
            foreach (var conflictCapabilityId in definition.ConflictsWith)
            {
                var conflictExists = definitions.Values.Any(candidate =>
                    candidate.Id.Equals(conflictCapabilityId, StringComparison.OrdinalIgnoreCase));

                if (!conflictExists)
                {
                    throw new InvalidOperationException(
                        $"Capability '{definition.Id}@{definition.Version}' conflictsWith references unknown capabilityId '{conflictCapabilityId}'.");
                }
            }

            if (definition.Prerequisite is null)
                continue;

            var prerequisiteExists = definitions.Values.Any(candidate =>
                candidate.Id.Equals(definition.Prerequisite.CapabilityId, StringComparison.OrdinalIgnoreCase));

            if (!prerequisiteExists)
            {
                throw new InvalidOperationException(
                    $"Capability '{definition.Id}@{definition.Version}' prerequisite references unknown capabilityId '{definition.Prerequisite.CapabilityId}'.");
            }
        }
    }

    private static string ResolveFilePath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new InvalidOperationException("Capability registry file path is required.");

        if (Path.IsPathRooted(configuredPath))
            return configuredPath;

        return Path.Combine(Directory.GetCurrentDirectory(), configuredPath);
    }

    private static string ToKey(string capabilityId, int version)
    {
        return $"{capabilityId.Trim().ToLowerInvariant()}@{version}";
    }

    private static string GetRequiredString(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var valueNode)
            || valueNode.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(valueNode.GetString()))
        {
            throw new InvalidOperationException($"Capability registry property '{propertyName}' is required.");
        }

        return valueNode.GetString()!.Trim();
    }

    private static int GetRequiredInt(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var valueNode)
            || valueNode.ValueKind != JsonValueKind.Number
            || !valueNode.TryGetInt32(out var value)
            || value <= 0)
        {
            throw new InvalidOperationException(
                $"Capability registry property '{propertyName}' must be a positive integer.");
        }

        return value;
    }

    private static TEnum GetRequiredEnum<TEnum>(JsonElement node, string propertyName)
        where TEnum : struct, Enum
    {
        var raw = GetRequiredString(node, propertyName);
        if (!Enum.TryParse<TEnum>(raw, ignoreCase: true, out var parsed))
        {
            throw new InvalidOperationException(
                $"Capability registry property '{propertyName}' has invalid value '{raw}'.");
        }

        return parsed;
    }

    private static string ResolveStateSchema(
        JsonElement entry,
        CapabilityRole role,
        string capabilityId,
        int version)
    {
        if (entry.TryGetProperty("stateSchema", out var stateSchemaNode)
            && stateSchemaNode.ValueKind == JsonValueKind.Object)
        {
            return stateSchemaNode.GetRawText();
        }

        if (role == CapabilityRole.Actuator)
            return "{}";

        throw new InvalidOperationException(
            $"Capability '{capabilityId}@{version}' has invalid stateSchema. Root must be object.");
    }

    private static string ResolveMetadata(JsonElement entry, string capabilityId, int version)
    {
        if (!entry.TryGetProperty("metadata", out var metadataNode))
            return "{}";

        if (metadataNode.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' has invalid metadata. Root must be object.");
        }

        return metadataNode.GetRawText();
    }

    private static Dictionary<string, CapabilityOperationDefinition> ParseOperations(
        JsonElement entry,
        CapabilityRole role,
        string capabilityId,
        int version)
    {
        if (!entry.TryGetProperty("operations", out var operationsNode))
        {
            if (role == CapabilityRole.Sensor)
                return new Dictionary<string, CapabilityOperationDefinition>(StringComparer.OrdinalIgnoreCase);

            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' has invalid operations. Root must be object.");
        }

        if (operationsNode.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' has invalid operations. Root must be object.");
        }

        var operations = new Dictionary<string, CapabilityOperationDefinition>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var operation in operationsNode.EnumerateObject())
        {
            if (string.IsNullOrWhiteSpace(operation.Name))
            {
                throw new InvalidOperationException(
                    $"Capability '{capabilityId}@{version}' contains an operation with an empty name.");
            }

            if (operation.Value.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"Capability '{capabilityId}@{version}' operation '{operation.Name}' schema must be an object.");
            }

            var operationName = operation.Name.Trim();
            var operationSchema = operation.Value.GetRawText();

            operations[operationName] = new CapabilityOperationDefinition(
                name: operationName,
                fullDefinitionSchema: operationSchema,
                commandSchema: operationSchema);
        }

        if (role != CapabilityRole.Sensor && operations.Count == 0)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' requires at least one operation for role '{role}'.");
        }

        if (role == CapabilityRole.Sensor && operations.Count > 0)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' with role 'Sensor' must not define operations.");
        }

        return operations;
    }

    private static IReadOnlyCollection<string> ParseConflictsWith(
        JsonElement entry,
        string capabilityId,
        int version)
    {
        if (!entry.TryGetProperty("conflictsWith", out var conflictsNode))
            return [];

        if (conflictsNode.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' conflictsWith must be an array.");
        }

        return conflictsNode
            .EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static CapabilityPrerequisiteDefinition? ParsePrerequisite(
        JsonElement entry,
        string capabilityId,
        int version)
    {
        if (!entry.TryGetProperty("prerequisite", out var prerequisiteNode))
            return null;

        if (prerequisiteNode.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' prerequisite must be an object.");
        }

        var prerequisiteCapabilityId = GetRequiredString(prerequisiteNode, "capabilityId");

        if (!prerequisiteNode.TryGetProperty("requiredState", out var requiredStateNode)
            || requiredStateNode.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' prerequisite.requiredState must be an object.");
        }

        var requiredState = requiredStateNode
            .EnumerateObject()
            .ToDictionary(
                item => item.Name,
                item => ConvertJsonElement(item.Value),
                StringComparer.OrdinalIgnoreCase);

        var autoFix = prerequisiteNode.TryGetProperty("autoFix", out var autoFixNode)
            && autoFixNode.ValueKind == JsonValueKind.True;

        return new CapabilityPrerequisiteDefinition(
            prerequisiteCapabilityId,
            requiredState,
            autoFix);
    }

    private static CapabilityApplyStrategyDefinition? ParseApplyStrategy(
        JsonElement entry,
        CapabilityRole role,
        string stateSchema,
        IReadOnlyDictionary<string, CapabilityOperationDefinition> operations,
        string capabilityId,
        int version)
    {
        if (!entry.TryGetProperty("applyStrategy", out var applyStrategyNode))
        {
            if (role == CapabilityRole.Control)
            {
                throw new InvalidOperationException(
                    $"Capability '{capabilityId}@{version}' with role 'Control' requires applyStrategy.");
            }

            return null;
        }

        if (role != CapabilityRole.Control)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' with role '{role}' must not define applyStrategy.");
        }

        if (applyStrategyNode.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' applyStrategy must be an object.");
        }

        var operation = GetRequiredString(applyStrategyNode, "operation");

        if (!operations.ContainsKey(operation))
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' applyStrategy.operation '{operation}' is not defined in operations.");
        }

        if (!applyStrategyNode.TryGetProperty("stateMapping", out var stateMappingNode)
            || stateMappingNode.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' applyStrategy.stateMapping must be an object.");
        }

        var stateMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var stateMappingEntry in stateMappingNode.EnumerateObject())
        {
            if (stateMappingEntry.Value.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(stateMappingEntry.Value.GetString()))
            {
                throw new InvalidOperationException(
                    $"Capability '{capabilityId}@{version}' applyStrategy.stateMapping.{stateMappingEntry.Name} must be a non-empty string.");
            }

            stateMapping[stateMappingEntry.Name] = stateMappingEntry.Value.GetString()!.Trim();
        }

        if (stateMapping.Count == 0)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' applyStrategy.stateMapping must contain at least one mapping.");
        }

        var readOnlyFields = ParseReadOnlyFields(applyStrategyNode, capabilityId, version);
        var partialUpdate = !applyStrategyNode.TryGetProperty("partialUpdate", out var partialUpdateNode)
            || partialUpdateNode.ValueKind != JsonValueKind.False;

        var stateProperties = ExtractSchemaPropertyNames(stateSchema);
        var operationProperties = ExtractSchemaPropertyNames(operations[operation].CommandSchema);

        var invalidStateMappingKeys = stateMapping.Keys
            .Where(key => !stateProperties.Contains(key))
            .ToList();
        if (invalidStateMappingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' applyStrategy.stateMapping keys are not declared in stateSchema: {string.Join(", ", invalidStateMappingKeys)}");
        }

        var invalidStateMappingValues = stateMapping.Values
            .Where(value => !operationProperties.Contains(value))
            .ToList();
        if (invalidStateMappingValues.Count > 0)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' applyStrategy.stateMapping values are not declared in operation '{operation}' schema: {string.Join(", ", invalidStateMappingValues)}");
        }

        var invalidReadOnlyFields = readOnlyFields
            .Where(field => !stateProperties.Contains(field))
            .ToList();
        if (invalidReadOnlyFields.Count > 0)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' applyStrategy.readOnlyFields are not declared in stateSchema: {string.Join(", ", invalidReadOnlyFields)}");
        }

        return new CapabilityApplyStrategyDefinition(
            operation,
            stateMapping,
            readOnlyFields,
            partialUpdate);
    }

    private static IReadOnlyCollection<string> ParseReadOnlyFields(
        JsonElement applyStrategyNode,
        string capabilityId,
        int version)
    {
        if (!applyStrategyNode.TryGetProperty("readOnlyFields", out var readOnlyFieldsNode))
            return [];

        if (readOnlyFieldsNode.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"Capability '{capabilityId}@{version}' applyStrategy.readOnlyFields must be an array.");
        }

        return readOnlyFieldsNode
            .EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HashSet<string> ExtractSchemaPropertyNames(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        if (!document.RootElement.TryGetProperty("properties", out var propertiesNode)
            || propertiesNode.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return propertiesNode
            .EnumerateObject()
            .Select(item => item.Name)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
    private static object? ConvertJsonElement(JsonElement element)
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
