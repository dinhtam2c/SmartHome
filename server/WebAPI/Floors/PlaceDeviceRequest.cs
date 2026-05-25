namespace WebAPI.Floors;

public sealed record PlaceDeviceRequest(Guid DeviceId, float X, float Y, Guid? FloorRoomId);
