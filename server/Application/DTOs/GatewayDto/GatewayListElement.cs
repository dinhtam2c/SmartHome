using Core.Entities;

namespace Application.DTOs.GatewayDto;

public record GatewayListElement(
    Guid Id,
    string? Name,
    string? HomeName,
    bool IsOnline,
    long LastSeenAt,
    long UpTime,
    int DeviceCount
)
{
    public static GatewayListElement FromGateway(Gateway gateway)
    {
        return new(
            Id: gateway.Id,
            Name: gateway.Name,
            HomeName: gateway.Home?.Name,
            IsOnline: gateway.IsOnline,
            LastSeenAt: gateway.LastSeenAt,
            UpTime: gateway.UpTime,
            DeviceCount: gateway.DeviceCount
        );
    }
}
