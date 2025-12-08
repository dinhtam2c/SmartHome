namespace Application.Exceptions;

public class GatewayNotFoundException : NotFoundException
{
    public GatewayNotFoundException(Guid gatewayId)
        : base($"Gateway {gatewayId} not found") { }
}
