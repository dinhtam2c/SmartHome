namespace Core.Domain.Scenes;

public sealed record SceneTargetDefinition(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    Dictionary<string, object?> DesiredState
);

public sealed record SceneSideEffectDefinition(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    string Operation,
    Dictionary<string, object?> Params,
    SceneSideEffectTiming Timing,
    int DelayMs
);

public enum SceneSideEffectTiming
{
    BeforeTargets,
    AfterDispatch,
    AfterVerify
}
