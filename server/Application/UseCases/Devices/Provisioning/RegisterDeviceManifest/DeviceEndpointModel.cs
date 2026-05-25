namespace Application.UseCases.Devices.Provisioning.RegisterDeviceManifest;

public sealed record DeviceEndpointModel(
    string EndpointId,
    string? Name,
    IEnumerable<DeviceCapabilityModel> Capabilities
);
