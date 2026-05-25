using Domain.Common;

namespace Domain.Models.Devices;

public record DeviceWentOnlineDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid HomeId,
    Guid? RoomId,
    long LastSeenAt
) : DomainEvent(Id);
