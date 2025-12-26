using Core.Entities;

namespace Application.DTOs.GatewayDto;

public record GatewayListElement(
    Guid Id,
    string? Name,
    string? HomeName
)
{
    public static GatewayListElement FromGateway(Gateway gateway)
    {
        return new(
            Id: gateway.Id,
            Name: gateway.Name,
            HomeName: gateway.Home?.Name
        );
    }
}
