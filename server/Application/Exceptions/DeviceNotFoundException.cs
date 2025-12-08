namespace Application.Exceptions;

public class DeviceNotFoundException : NotFoundException
{
    public DeviceNotFoundException(Guid deviceId)
        : base($"Device with id {deviceId} not found") { }

    public DeviceNotFoundException(string identifier)
        : base($"Device with identifier {identifier} not found") { }
}
