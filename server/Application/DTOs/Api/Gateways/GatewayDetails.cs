using Core.Entities;

namespace Application.DTOs.Api.Gateways;

public record GatewayDetails(
    Guid Id,
    string Name,
    Guid? HomeId,
    string? HomeName,
    string? Manufacturer,
    string? Model,
    string FirmwareVersion,
    string Mac,
    long CreatedAt,
    long UpdatedAt
)
{
    public static GatewayDetails FromGateway(Gateway gateway)
    {
        return new(
            Id: gateway.Id,
            Name: gateway.Name,
            HomeId: gateway.HomeId,
            HomeName: gateway.Home?.Name,
            Manufacturer: gateway.Manufacturer,
            Model: gateway.Model,
            FirmwareVersion: gateway.FirmwareVersion,
            Mac: gateway.Mac,
            CreatedAt: gateway.CreatedAt,
            UpdatedAt: gateway.UpdatedAt
        );
    }
}
