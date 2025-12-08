using Core.Entities;

namespace Application.DTOs.DeviceDto;

public record DeviceListElement(
    Guid Id,
    string Identifier,
    string Name,
    string? GatewayName,
    string? Home,
    string? Location,
    bool IsOnline,
    long LastSeenAt,
    long UpTime
)
{
    public static DeviceListElement FromDevice(Device device)
    {
        return new(
            Id: device.Id,
            Identifier: device.Identifier,
            Name: device.Name,
            GatewayName: device.Gateway?.Name,
            Home: device.Gateway?.Home?.Name,
            Location: device.Location?.Name,
            IsOnline: device.IsOnline,
            LastSeenAt: device.LastSeenAt,
            UpTime: device.UpTime
        );
    }
}
