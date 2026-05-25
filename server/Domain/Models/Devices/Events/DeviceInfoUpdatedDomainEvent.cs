using Domain.Common;

namespace Domain.Models.Devices;

public record DeviceInfoUpdatedDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid? HomeId,
    Guid? RoomId,
    string Name
) : DomainEvent(Id);
