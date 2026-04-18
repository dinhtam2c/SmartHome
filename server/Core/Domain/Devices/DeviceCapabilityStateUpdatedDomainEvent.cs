using Core.Primitives;

namespace Core.Domain.Devices;

public record DeviceCapabilityStateUpdatedDomainEvent(
    Guid Id,
    Guid DeviceId,
    string CapabilityId,
    string EndpointId,
    long ReportedAt,
    Dictionary<string, object?> State
) : DomainEvent(Id);
