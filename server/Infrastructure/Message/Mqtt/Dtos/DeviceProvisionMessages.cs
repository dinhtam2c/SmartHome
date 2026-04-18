using Core.Domain.Devices;

namespace Infrastructure.Message.Mqtt.Dtos;

public record DeviceProvisionResponseMessage(
    string ProvisionCode
);

public record DeviceCredentialsResponseMessage(
    Guid DeviceId,
    string AccessToken
);

public record ProvisionDeviceMessage(
    string Name,
    string FirmwareVersion,
    DeviceProtocol Protocol,
    IEnumerable<DeviceEndpointMessage> Endpoints
);

public record DeviceEndpointMessage(
    string EndpointId,
    string? Name,
    IEnumerable<DeviceCapabilityMessage> Capabilities
);

public record DeviceCapabilityMessage(
    string CapabilityId,
    int CapabilityVersion,
    IEnumerable<string>? SupportedOperations
);
