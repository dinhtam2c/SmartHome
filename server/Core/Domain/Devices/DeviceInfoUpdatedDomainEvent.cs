using Core.Primitives;

namespace Core.Domain.Devices;

public record DeviceInfoUpdatedDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid? HomeId,
    Guid? RoomId,
    string Name
) : DomainEvent(Id);
