namespace Domain.Models.ActionSets;

public sealed record ActionTarget(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId
);
