namespace Application.Commands.Devices.ProvisionDevice;

public sealed record DeviceEndpointModel(
    string EndpointId,
    string? Name,
    IEnumerable<DeviceCapabilityModel> Capabilities
);
