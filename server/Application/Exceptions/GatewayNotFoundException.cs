namespace Application.Exceptions;

public class GatewayNotFoundException : NotFoundException
{
    public GatewayNotFoundException(Guid gatewayId)
        : base($"Gateway with id {gatewayId} not found") { }
}
