using Domain.Models.Devices;

namespace Application.UseCases.Devices.Provisioning.RegisterDeviceManifest;

public record DeviceCapabilityModel(
    string CapabilityId,
    int CapabilityVersion,
    IEnumerable<string>? SupportedOperations
)
{
    public DeviceCapability ToDeviceCapability(Guid endpointId)
    {
        return new(
            endpointId: endpointId,
            capabilityId: CapabilityId,
            capabilityVersion: CapabilityVersion,
            supportedOperations: SupportedOperations
        );
    }
}
