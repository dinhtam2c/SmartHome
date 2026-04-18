using System.Text.Json;
using Application.Services;

namespace WebAPI.Capabilities;

public static class CapabilityRegistryEndpoints
{
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {
        var capabilityApi = routes.MapGroup("/capabilities");
        capabilityApi.MapGet("/registry", GetRegistry);
    }

    private static IResult GetRegistry(ICapabilityRegistry capabilityRegistry)
    {
        var response = capabilityRegistry.GetAll()
            .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(definition => definition.Version)
            .Select(definition => new CapabilityRegistryEntryResponse(
                definition.Id,
                definition.Version,
                definition.Role.ToString(),
                ParseJson(definition.StateSchema),
                definition.Operations.ToDictionary(
                    operation => operation.Key,
                    operation => ParseJson(operation.Value.FullDefinitionSchema),
                    StringComparer.OrdinalIgnoreCase),
                ParseJson(definition.Metadata),
                definition.ConflictsWith.ToList(),
                definition.Prerequisite is null
                    ? null
                    : new CapabilityRegistryPrerequisiteResponse(
                        definition.Prerequisite.CapabilityId,
                        ToJsonElement(definition.Prerequisite.RequiredState),
                        definition.Prerequisite.AutoFix),
                definition.ApplyStrategy is null
                    ? null
                    : new CapabilityRegistryApplyStrategyResponse(
                        definition.ApplyStrategy.Operation,
                        definition.ApplyStrategy.StateMapping.ToDictionary(
                            item => item.Key,
                            item => item.Value,
                            StringComparer.OrdinalIgnoreCase),
                        definition.ApplyStrategy.ReadOnlyFields.ToList(),
                        definition.ApplyStrategy.PartialUpdate)))
            .ToList();

        return Results.Ok(response);
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static JsonElement ToJsonElement(object value)
    {
        return JsonSerializer.SerializeToElement(value);
    }

    private sealed record CapabilityRegistryEntryResponse(
        string Id,
        int Version,
        string Role,
        JsonElement StateSchema,
        Dictionary<string, JsonElement> Operations,
        JsonElement Metadata,
        IReadOnlyList<string> ConflictsWith,
        CapabilityRegistryPrerequisiteResponse? Prerequisite,
        CapabilityRegistryApplyStrategyResponse? ApplyStrategy);

    private sealed record CapabilityRegistryPrerequisiteResponse(
        string CapabilityId,
        JsonElement RequiredState,
        bool AutoFix);

    private sealed record CapabilityRegistryApplyStrategyResponse(
        string Operation,
        Dictionary<string, string> StateMapping,
        IReadOnlyList<string> ReadOnlyFields,
        bool PartialUpdate);
}
