using Core.Domain.Scenes;

namespace Application.Commands.Scenes;

public sealed record SceneTargetModel(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    Dictionary<string, object?> DesiredState
);

public sealed record SceneSideEffectModel(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    string Operation,
    Dictionary<string, object?> Params,
    SceneSideEffectTiming Timing,
    int DelayMs
);
