using Application.Ports.Registries;
using Application.Common.Errors;
using Application.Common.Serialization;
using Domain.Models.Devices;

namespace Application.BusinessServices.Capabilities.Validation;

public interface ICapabilityStateValidator
{
    Dictionary<string, object?> NormalizeAndValidate(
        DeviceCapability capability,
        IReadOnlyDictionary<string, object?> state);
}

public sealed class CapabilityStateValidator : ICapabilityStateValidator
{
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly IJsonSchemaPayloadEvaluator _schemaEvaluator;

    public CapabilityStateValidator(
        ICapabilityRegistry capabilityRegistry,
        IJsonSchemaPayloadEvaluator schemaEvaluator)
    {
        _capabilityRegistry = capabilityRegistry;
        _schemaEvaluator = schemaEvaluator;
    }

    public Dictionary<string, object?> NormalizeAndValidate(
        DeviceCapability capability,
        IReadOnlyDictionary<string, object?> state)
    {
        Dictionary<string, object?> normalized;
        try
        {
            normalized = JsonValueNormalizer.NormalizeObject(state);
        }
        catch (Exception ex)
        {
            throw new InvalidCapabilityStatePayloadException(
                capability.CapabilityId,
                $"payload cannot be normalized: {ex.Message}");
        }

        if (!_capabilityRegistry.TryGetDefinition(
                capability.CapabilityId,
                capability.CapabilityVersion,
                out var definition))
        {
            throw new InvalidCapabilityStatePayloadException(
                capability.CapabilityId,
                $"registry definition '{capability.CapabilityId}@{capability.CapabilityVersion}' was not found");
        }

        bool isValid;
        try
        {
            isValid = _schemaEvaluator.IsValid(definition.StateSchema, normalized);
        }
        catch (Exception ex)
        {
            throw new InvalidCapabilityStatePayloadException(
                capability.CapabilityId,
                $"state schema cannot be parsed: {ex.GetBaseException().Message}");
        }

        if (!isValid)
        {
            throw new InvalidCapabilityStatePayloadException(
                capability.CapabilityId,
                "payload does not match state schema");
        }

        return normalized;
    }
}
