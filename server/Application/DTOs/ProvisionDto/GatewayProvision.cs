using Core.Entities;

namespace Application.DTOs.ProvisionDto;

public record GatewayProvisionRequest(
    string Key,
    string Mac,
    string Name,
    string? Manufacturer,
    string? Model,
    string FirmwareVersion,
    long Timestamp
)
{
    public Gateway ToGateway()
    {
        return new(
            id: Guid.NewGuid(),
            name: Name,
            manufacturer: Manufacturer,
            model: Model,
            firmwareVersion: FirmwareVersion,
            mac: Mac,
            createdAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
    }
}

public record GatewayProvisionResponse(
    Guid? GatewayId
);
