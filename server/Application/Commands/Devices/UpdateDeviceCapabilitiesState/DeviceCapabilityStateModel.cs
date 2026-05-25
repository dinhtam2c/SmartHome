namespace Application.Commands.Devices.UpdateDeviceCapabilitiesState;

public sealed record DeviceCapabilityStateModel(
    string CapabilityId,
    string EndpointId,
    Dictionary<string, object?> State
);
