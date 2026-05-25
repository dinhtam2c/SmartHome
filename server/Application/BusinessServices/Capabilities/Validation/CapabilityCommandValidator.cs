using Application.Ports.Registries;
using Application.Common.Errors;
using Application.Common.Serialization;
using Domain.Models.Capabilities;
using Domain.Models.Devices;

namespace Application.BusinessServices.Capabilities.Validation;

public interface ICapabilityCommandValidator
{
    object? NormalizeAndValidate(DeviceCapability capability, string operation, object? value);
}

public class CapabilityCommandValidator : ICapabilityCommandValidator
{
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly IJsonSchemaPayloadEvaluator _schemaEvaluator;

    public CapabilityCommandValidator(
        ICapabilityRegistry capabilityRegistry,
        IJsonSchemaPayloadEvaluator schemaEvaluator)
    {
        _capabilityRegistry = capabilityRegistry;
        _schemaEvaluator = schemaEvaluator;
    }

    public object? NormalizeAndValidate(DeviceCapability capability, string operation, object? value)
    {
        object? normalized;
        try
        {
            normalized = JsonValueNormalizer.Normalize(value);
        }
        catch (Exception ex)
        {
            throw new InvalidCapabilityCommandPayloadException(
                capability.CapabilityId,
                $"payload cannot be normalized: {ex.Message}");
        }

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

    private void ValidateBySchema(string capabilityId, string operation, string schemaJson, object? value)
    {
        bool isValid;
        try
        {
            isValid = _schemaEvaluator.IsValid(schemaJson, value);
        }
        catch (Exception ex)
        {
            throw new InvalidCapabilityCommandPayloadException(
                capabilityId,
                $"operation schema '{operation}' cannot be parsed: {ex.GetBaseException().Message}");
        }

        if (!isValid)
        {
            throw new InvalidCapabilityCommandPayloadException(
                capabilityId,
                $"payload does not match JSON schema for operation '{operation}'");
        }
    }
}
