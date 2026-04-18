using Core.Domain.Devices;

namespace Application.Commands.Scenes.ExecuteScene;

public sealed record ScenePlanningRequest(
    DeviceCapability Capability,
    CapabilityDefinition Definition,
    Dictionary<string, object?> DesiredState);

public sealed record PlannedSceneCommand(
    string Operation,
    object? Value);

public interface IScenePlanner
{
    bool TryPlan(
        ScenePlanningRequest request,
        out PlannedSceneCommand? command,
        out string? error);
}
