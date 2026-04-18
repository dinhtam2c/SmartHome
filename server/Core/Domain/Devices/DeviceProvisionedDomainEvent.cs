using Core.Primitives;

namespace Core.Domain.Devices;

public record DeviceProvisionedDomainEvent(
    Guid Id,
    string MacAddress,
    Guid DeviceId,
    string AccessToken
) : DomainEvent(Id);
