using Domain.Common;

namespace Domain.Models.Devices;

public record DeviceRoomAssignedDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid HomeId,
    Guid? PreviousRoomId,
    Guid? RoomId
) : DomainEvent(Id);
