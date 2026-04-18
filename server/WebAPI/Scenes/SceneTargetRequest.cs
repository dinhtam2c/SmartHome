namespace WebAPI.Scenes;

public sealed record SceneTargetRequest(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    Dictionary<string, object?> DesiredState
);
