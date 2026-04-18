namespace Application.Exceptions;

public class DeviceNotFoundException : NotFoundException
{
    public DeviceNotFoundException(Guid deviceId)
        : base($"Device with id {deviceId} not found") { }

    public DeviceNotFoundException(string macAddress)
        : base($"Device with mac address {macAddress} not found") { }

    public DeviceNotFoundException()
        : base($"Device not found") { }
}
