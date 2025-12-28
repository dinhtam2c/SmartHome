using Core.Entities;

namespace Application.DTOs.Messages.Provision;

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
            name: Name,
            manufacturer: Manufacturer,
            model: Model,
            firmwareVersion: FirmwareVersion,
            mac: Mac
        );
    }
}

public record GatewayProvisionResponse(
    Guid? GatewayId
);
