namespace WebAPI.Devices;

public sealed record AddDeviceRequest(Guid HomeId, Guid? RoomId, string ProvisionCode);
