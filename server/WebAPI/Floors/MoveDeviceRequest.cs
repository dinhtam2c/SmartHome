namespace WebAPI.Floors;

public sealed record MoveDeviceRequest(float X, float Y, Guid? FloorRoomId);
