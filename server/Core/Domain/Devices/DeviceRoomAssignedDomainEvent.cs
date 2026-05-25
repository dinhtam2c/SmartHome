using Core.Primitives;

namespace Core.Domain.Devices;

public record DeviceRoomAssignedDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid HomeId,
    Guid? PreviousRoomId,
    Guid? RoomId
) : DomainEvent(Id);
