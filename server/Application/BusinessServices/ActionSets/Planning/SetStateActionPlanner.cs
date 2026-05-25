using Application.BusinessServices.Capabilities.Validation;
using Application.BusinessServices.ActionSets.Planning;
using Domain.Models.Devices;

namespace Application.BusinessServices.ActionSets.Planning;

public sealed class SetStateActionPlanner : ISetStateActionPlanner
{
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;

    public SetStateActionPlanner(ICapabilityCommandValidator capabilityCommandValidator)
    {
        _capabilityCommandValidator = capabilityCommandValidator;
    }

    public bool TryPlan(
        SetStateActionPlanningRequest request,
        out PlannedSetStateActionCommand? command,
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
            error = "Capability has no supported operations for set-state execution.";
            return false;
        }

        var strategy = request.Definition.ApplyStrategy;
        if (strategy is null)
        {
            command = null;
            error = "Capability has no applyStrategy for set-state planning.";
            return false;
        }

        if (!supportedOperations.Contains(strategy.Operation, StringComparer.OrdinalIgnoreCase))
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

        var mappedPayloadResult = ActionStateHelper.BuildApplyStrategyPayload(request.State, strategy);
        if (!mappedPayloadResult.Success)
        {
            command = null;
            error = mappedPayloadResult.Error;
            return false;
        }

        try
        {
            var normalizedPayload = _capabilityCommandValidator.NormalizeAndValidate(
                request.Capability,
                strategy.Operation,
                mappedPayloadResult.Payload);

            command = new PlannedSetStateActionCommand(strategy.Operation, normalizedPayload);
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
}
