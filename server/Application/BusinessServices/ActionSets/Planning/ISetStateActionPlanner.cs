using Domain.Models.Capabilities;
using Domain.Models.Devices;

namespace Application.BusinessServices.ActionSets.Planning;

public sealed record SetStateActionPlanningRequest(
    DeviceCapability Capability,
    CapabilityDefinition Definition,
    Dictionary<string, object?> State);

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
