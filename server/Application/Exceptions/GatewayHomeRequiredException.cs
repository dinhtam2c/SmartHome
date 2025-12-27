namespace Application.Exceptions;

public class GatewayHomeRequiredException : BadRequestException
{
    public GatewayHomeRequiredException(Guid gatewayId)
        : base($"Gateway {gatewayId} must be assigned to a home before device can be assigned to a location") { }
}
