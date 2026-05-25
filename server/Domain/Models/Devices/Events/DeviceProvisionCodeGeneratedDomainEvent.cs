using Domain.Common;

namespace Domain.Models.Devices;

public record DeviceProvisionCodeGeneratedDomainEvent(
    Guid Id,
    string MacAddress,
    string ProvisionCode
) : DomainEvent(Id);
