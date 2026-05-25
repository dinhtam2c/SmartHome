using Core.Primitives;

namespace Core.Domain.Devices;

public record DeviceProvisionedDomainEvent(
    Guid Id,
    string MacAddress,
    Guid DeviceId,
    Guid HomeId,
    Guid? RoomId,
    string AccessToken
) : DomainEvent(Id);
