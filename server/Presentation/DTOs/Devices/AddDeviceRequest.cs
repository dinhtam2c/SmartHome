namespace Presentation.Devices;

public sealed record AddDeviceRequest(Guid HomeId, Guid? RoomId, string ProvisionCode);
