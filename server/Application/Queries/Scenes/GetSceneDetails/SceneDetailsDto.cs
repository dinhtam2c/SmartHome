using Core.Domain.Scenes;

namespace Application.Queries.Scenes.GetSceneDetails;

public sealed record SceneDetailsDto(
    Guid Id,
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    long CreatedAt,
    long UpdatedAt,
    IReadOnlyList<SceneTargetDetailsDto> Targets,
    IReadOnlyList<SceneSideEffectDetailsDto> SideEffects
);

public sealed record SceneTargetDetailsDto(
    Guid Id,
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    Dictionary<string, object?> DesiredState,
    int Order
);

public sealed record SceneSideEffectDetailsDto(
    Guid Id,
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    string Operation,
    Dictionary<string, object?> Params,
    SceneSideEffectTiming Timing,
    int DelayMs,
    int Order
);
