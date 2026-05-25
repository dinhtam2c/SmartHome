using Core.Primitives;

namespace Core.Domain.Devices;

public record DeviceSystemStateUpdatedDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid HomeId,
    Guid? RoomId,
    long Uptime,
    long LastSeenAt
) : DomainEvent(Id);
