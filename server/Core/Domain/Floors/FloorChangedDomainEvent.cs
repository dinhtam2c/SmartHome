using Core.Primitives;

namespace Core.Domain.Floors;

public sealed record FloorChangedDomainEvent(
    Guid Id,
    Guid FloorId,
    Guid HomeId,
    string Reason
) : DomainEvent(Id);

public static class FloorChangeReasons
{
    public const string Created = "Created";
    public const string Deleted = "Deleted";
    public const string InfoUpdated = "InfoUpdated";
    public const string RoomAdded = "RoomAdded";
    public const string RoomUpdated = "RoomUpdated";
    public const string RoomRemoved = "RoomRemoved";
    public const string DevicePlaced = "DevicePlaced";
    public const string DeviceMoved = "DeviceMoved";
    public const string DeviceRemoved = "DeviceRemoved";
    public const string LinkedRoomDeleted = "LinkedRoomDeleted";
    public const string DeviceDeleted = "DeviceDeleted";
}
