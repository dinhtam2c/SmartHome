namespace Application.Exceptions;

public class DeviceGatewayRequiredException : BadRequestException
{
    public DeviceGatewayRequiredException(Guid deviceId)
        : base($"Device {deviceId} must be assigned to a gateway before assigning a location") { }
}
