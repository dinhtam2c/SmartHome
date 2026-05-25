using Domain.Common;

namespace Domain.Models.Devices;

public record DeviceWentOfflineDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid HomeId,
    Guid? RoomId,
    long LastSeenAt
) : DomainEvent(Id);
