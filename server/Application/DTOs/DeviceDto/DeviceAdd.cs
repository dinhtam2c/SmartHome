using Core.Entities;

namespace Application.DTOs.DeviceDto;

public record DeviceAddRequest(
    Guid? GatewayId,
    string Identifier,
    string? Name
)
{
    public Device ToDevice()
    {
        return new(
            id: Guid.NewGuid(),
            gatewayId: GatewayId,
            identifier: Identifier,
            name: Name ?? "",
            createdAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
    }
}

public record DeviceAddResponse(
    Guid DeviceId,
    Guid? GatewayId,
    string Identifier,
    string Name
)
{
    public static DeviceAddResponse FromDevice(Device device)
    {
        return new DeviceAddResponse(
            DeviceId: device.Id,
            GatewayId: device.GatewayId,
            Identifier: device.Identifier,
            Name: device.Name
        );
    }
}
