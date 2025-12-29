namespace Application.Exceptions;

public class DeviceGatewayMismatchException : BadRequestException
{
    public DeviceGatewayMismatchException(Guid expectedGatewayId, Guid actualGatewayId)
        : base($"Device provisioning failed: Gateway ID mismatch. Expected {expectedGatewayId}, but got {actualGatewayId}") { }

    public DeviceGatewayMismatchException(string message)
        : base(message) { }
}
