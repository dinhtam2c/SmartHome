using System.Text.Json;
using Application.Exceptions;
using Core.Domain.Devices;
using Json.Schema;

namespace Application.Services;

public interface ICapabilityCommandValidator
{
    object? ValidateAndNormalize(DeviceCapability capability, string operation, object? value);
}

public class CapabilityCommandValidator : ICapabilityCommandValidator
{
    private readonly ICapabilityRegistry _capabilityRegistry;

    public CapabilityCommandValidator(ICapabilityRegistry capabilityRegistry)
    {
        _capabilityRegistry = capabilityRegistry;
    }

    public object? ValidateAndNormalize(DeviceCapability capability, string operation, object? value)
    {
        var normalized = NormalizeJsonValue(value);

        if (!_capabilityRegistry.TryGetDefinition(
                capability.CapabilityId,
                capability.CapabilityVersion,
                out var definition))
        {
            throw new InvalidCapabilityCommandPayloadException(
                capability.CapabilityId,
                $"registry definition '{capability.CapabilityId}@{capability.CapabilityVersion}' was not found");
        }

        var operationSchema = ResolveOperationSchema(capability.CapabilityId, operation, definition);
        ValidateBySchema(capability.CapabilityId, operation, operationSchema, normalized);

        return normalized;
    }

    private static string ResolveOperationSchema(
        string capabilityId,
        string operation,
        CapabilityDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new InvalidCapabilityCommandPayloadException(
                capabilityId,
                "operation is required for command schema validation");
        }

        if (!definition.TryGetOperation(operation, out var operationDefinition))
        {
            throw new InvalidCapabilityCommandPayloadException(
                capabilityId,
                $"operation '{operation}' is not defined in registry operations");
        }

        return operationDefinition.CommandSchema;
    }

    private static void ValidateBySchema(string capabilityId, string operation, string schemaJson, object? value)
    {
        JsonSchema schema;
        try
        {
            schema = JsonSerializer.Deserialize<JsonSchema>(schemaJson)
                ?? throw new InvalidOperationException("schema is empty");
        }
        catch (Exception ex)
        {
            throw new InvalidCapabilityCommandPayloadException(
                capabilityId,
                $"operation schema '{operation}' cannot be parsed: {ex.Message}");
        }

        var payloadNode = JsonSerializer.SerializeToNode(value);
        var result = schema.Evaluate(payloadNode);

        if (!result.IsValid)
        {
            throw new InvalidCapabilityCommandPayloadException(
                capabilityId,
                $"payload does not match JSON schema for operation '{operation}'");
        }
    }

    private static object? NormalizeJsonValue(object? input)
    {
        if (input is not JsonElement element)
            return input;

        return ConvertJsonElement(element);
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
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
