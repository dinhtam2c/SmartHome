using Domain.Common;

namespace Domain.Models.Devices;

public record DeviceDeletedDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid? HomeId,
    Guid? RoomId,
    bool IsOnline
) : DomainEvent(Id);
