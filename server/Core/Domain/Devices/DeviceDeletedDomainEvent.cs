using Core.Primitives;

namespace Core.Domain.Devices;

public record DeviceDeletedDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid? HomeId,
    Guid? RoomId
) : DomainEvent(Id);
