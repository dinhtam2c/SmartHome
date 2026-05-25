using Core.Domain.Devices;

namespace Application.ActionSets.Planning;

public sealed record SetStateActionPlanningRequest(
    DeviceCapability Capability,
    CapabilityDefinition Definition,
    Dictionary<string, object?> State,
    Dictionary<string, object?> Options);

public sealed record PlannedSetStateActionCommand(
    string Operation,
    object? Value);

public interface ISetStateActionPlanner
{
    bool TryPlan(
        SetStateActionPlanningRequest request,
        out PlannedSetStateActionCommand? command,
        out string? error);
}
