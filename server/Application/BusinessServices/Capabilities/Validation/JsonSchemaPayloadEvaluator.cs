using System.Collections.Concurrent;
using System.Text.Json;
using Json.Schema;

namespace Application.BusinessServices.Capabilities.Validation;

public interface IJsonSchemaPayloadEvaluator
{
    void EnsureSchemaIsValid(string schemaJson);

    bool IsValid(string schemaJson, object? payload);
}

public sealed class JsonSchemaPayloadEvaluator : IJsonSchemaPayloadEvaluator
{
    private readonly ConcurrentDictionary<string, JsonSchema> _schemas = new();

    public void EnsureSchemaIsValid(string schemaJson)
    {
        _ = GetSchema(schemaJson);
    }

    public bool IsValid(string schemaJson, object? payload)
    {
        var schema = GetSchema(schemaJson);
        var payloadElement = JsonSerializer.SerializeToElement(payload);
        return schema.Evaluate(payloadElement).IsValid;
    }

    private JsonSchema GetSchema(string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            throw new InvalidOperationException("Schema is empty.");

        return _schemas.GetOrAdd(
            schemaJson,
            static json => JsonSerializer.Deserialize<JsonSchema>(json)
                ?? throw new InvalidOperationException("Schema is empty."));
    }
}
