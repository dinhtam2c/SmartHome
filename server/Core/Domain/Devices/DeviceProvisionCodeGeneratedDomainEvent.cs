using Core.Primitives;

namespace Core.Domain.Devices;

public record DeviceProvisionCodeGeneratedDomainEvent(
    Guid Id,
    string MacAddress,
    string ProvisionCode
) : DomainEvent(Id);
