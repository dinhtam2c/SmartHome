using Application.Services;
using Core.Domain.Devices;

namespace Application.Commands.Scenes.ExecuteScene;

public sealed class ScenePlanner : IScenePlanner
{
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;

    public ScenePlanner(ICapabilityCommandValidator capabilityCommandValidator)
    {
        _capabilityCommandValidator = capabilityCommandValidator;
    }

    public bool TryPlan(
        ScenePlanningRequest request,
        out PlannedSceneCommand? command,
        out string? error)
    {
        var supportedOperations = request.Capability.SupportedOperations?
            .Where(operation => !string.IsNullOrWhiteSpace(operation))
            .Select(operation => operation.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? [];

        if (supportedOperations.Count == 0)
        {
            command = null;
            error = "Capability has no supported operations for scene execution.";
            return false;
        }

        var strategy = request.Definition.ApplyStrategy;
        if (strategy is null)
        {
            command = null;
            error = "Capability has no applyStrategy for state-based scene planning.";
            return false;
        }

        var supportsMappedOperation = supportedOperations
            .Contains(strategy.Operation, StringComparer.OrdinalIgnoreCase);
        if (!supportsMappedOperation)
        {
            command = null;
            error = $"Capability does not support applyStrategy operation '{strategy.Operation}'.";
            return false;
        }

        if (!request.Definition.TryGetOperation(strategy.Operation, out _))
        {
            command = null;
            error = $"applyStrategy operation '{strategy.Operation}' is missing from capability definition.";
            return false;
        }

        var mappedPayloadResult = BuildApplyStrategyPayload(
            request.DesiredState,
            strategy);
        if (!mappedPayloadResult.Success)
        {
            command = null;
            error = mappedPayloadResult.Error;
            return false;
        }

        try
        {
            var normalizedPayload = _capabilityCommandValidator.ValidateAndNormalize(
                request.Capability,
                strategy.Operation,
                mappedPayloadResult.Payload);

            command = new PlannedSceneCommand(strategy.Operation, normalizedPayload);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            command = null;
            error = $"applyStrategy operation '{strategy.Operation}' payload is invalid: {ex.Message}";
            return false;
        }
    }

    private (bool Success, Dictionary<string, object?> Payload, string? Error) BuildApplyStrategyPayload(
        IReadOnlyDictionary<string, object?> desiredState,
        CapabilityApplyStrategyDefinition strategy)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in strategy.StateMapping)
        {
            if (!desiredState.TryGetValue(mapping.Key, out var desiredValue))
            {
                if (strategy.PartialUpdate)
                    continue;

                return (
                    false,
                    payload,
                    $"applyStrategy.partialUpdate=false requires state field '{mapping.Key}' to be present in desiredState.");
            }

            payload[mapping.Value] = desiredValue;
        }

        return (true, payload, null);
    }
}
