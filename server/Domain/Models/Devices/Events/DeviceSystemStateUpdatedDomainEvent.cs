using Domain.Common;

namespace Domain.Models.Devices;

public record DeviceSystemStateUpdatedDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid HomeId,
    Guid? RoomId,
    long Uptime,
    long LastSeenAt
) : DomainEvent(Id);
