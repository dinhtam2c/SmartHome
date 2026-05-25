using Domain.Common;

namespace Application.Common.Realtime;

public static class RealtimeEntities
{
    public const string AutomationExecution = "AutomationExecution";
    public const string AutomationRule = "AutomationRule";
    public const string Device = "Device";
    public const string DeviceCapability = "DeviceCapability";
    public const string Floor = "Floor";
    public const string Room = "Room";
    public const string Scene = "Scene";
    public const string SceneExecution = "SceneExecution";
}

public static class RealtimeChanges
{
    public const string Created = "Created";
    public const string Deleted = "Deleted";
    public const string Moved = "Moved";
    public const string StateChanged = "StateChanged";
    public const string StatusChanged = "StatusChanged";
    public const string Updated = "Updated";
}

public sealed record RealtimeDelta(
    int Version,
    string Entity,
    string Change,
    long OccurredAt,
    Guid? HomeId = null,
    Guid? RoomId = null,
    Guid? PreviousRoomId = null,
    Guid? DeviceId = null,
    Guid? FloorId = null,
    Guid? SceneId = null,
    Guid? RuleId = null,
    Guid? ExecutionId = null,
    string? EndpointId = null,
    string? CapabilityId = null,
    object? Delta = null)
{
    public const int CurrentVersion = 1;

    public static RealtimeDelta Create(
        string entity,
        string change,
        Guid? homeId = null,
        Guid? roomId = null,
        Guid? previousRoomId = null,
        Guid? deviceId = null,
        Guid? floorId = null,
        Guid? sceneId = null,
        Guid? ruleId = null,
        Guid? executionId = null,
        string? endpointId = null,
        string? capabilityId = null,
        object? delta = null)
    {
        return new RealtimeDelta(
            Version: CurrentVersion,
            Entity: entity,
            Change: change,
            OccurredAt: UnixTime.Now(),
            HomeId: homeId,
            RoomId: roomId,
            PreviousRoomId: previousRoomId,
            DeviceId: deviceId,
            FloorId: floorId,
            SceneId: sceneId,
            RuleId: ruleId,
            ExecutionId: executionId,
            EndpointId: Normalize(endpointId),
            CapabilityId: Normalize(capabilityId),
            Delta: delta);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
