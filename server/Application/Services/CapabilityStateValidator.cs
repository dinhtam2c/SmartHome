using System.Text.Json;
using Application.Exceptions;
using Core.Domain.Devices;
using Json.Schema;

namespace Application.Services;

public interface ICapabilityStateValidator
{
    void Validate(DeviceCapability capability, IReadOnlyDictionary<string, object?> state);
}

public sealed class CapabilityStateValidator : ICapabilityStateValidator
{
    private readonly ICapabilityRegistry _capabilityRegistry;

    public CapabilityStateValidator(ICapabilityRegistry capabilityRegistry)
    {
        _capabilityRegistry = capabilityRegistry;
    }

    public void Validate(DeviceCapability capability, IReadOnlyDictionary<string, object?> state)
    {
        if (!_capabilityRegistry.TryGetDefinition(
                capability.CapabilityId,
                capability.CapabilityVersion,
                out var definition))
        {
            throw new InvalidCapabilityStatePayloadException(
                capability.CapabilityId,
                $"registry definition '{capability.CapabilityId}@{capability.CapabilityVersion}' was not found");
        }

        JsonSchema schema;
        try
        {
            schema = JsonSerializer.Deserialize<JsonSchema>(definition.StateSchema)
                ?? throw new InvalidOperationException("State schema is empty");
        }
        catch (Exception ex)
        {
            throw new InvalidCapabilityStatePayloadException(
                capability.CapabilityId,
                $"state schema cannot be parsed: {ex.Message}");
        }

        var stateNode = JsonSerializer.SerializeToNode(state);
        var result = schema.Evaluate(stateNode);

        if (!result.IsValid)
        {
            throw new InvalidCapabilityStatePayloadException(
                capability.CapabilityId,
                "payload does not match state schema");
        }
    }
}
