using Core.Domain.Scenes;

namespace WebAPI.Scenes;

public sealed record SceneSideEffectRequest(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    string Operation,
    Dictionary<string, object?> Params,
    SceneSideEffectTiming Timing,
    int DelayMs
);
