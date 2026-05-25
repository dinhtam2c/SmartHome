namespace Application.BusinessServices.Devices.State;

public sealed record CapabilityStateUpdate(
    string CapabilityId,
    string EndpointId,
    Dictionary<string, object?> State);
