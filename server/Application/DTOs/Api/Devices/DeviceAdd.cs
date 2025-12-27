using Core.Entities;

namespace Application.DTOs.Api.Devices;

public record DeviceAddRequest(
    Guid? GatewayId,
    string Identifier,
    string? Name
)
{
    public Device ToDevice()
    {
        return new(
            gatewayId: GatewayId,
            identifier: Identifier,
            name: Name ?? ""
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
