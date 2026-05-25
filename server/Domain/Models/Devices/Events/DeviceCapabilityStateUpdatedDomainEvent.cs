using Domain.Common;

namespace Domain.Models.Devices;

public record DeviceCapabilityStateUpdatedDomainEvent(
    Guid Id,
    Guid DeviceId,
    Guid? HomeId,
    Guid? RoomId,
    string CapabilityId,
    string EndpointId,
    long ReportedAt,
    Dictionary<string, object?> State
) : DomainEvent(Id);
