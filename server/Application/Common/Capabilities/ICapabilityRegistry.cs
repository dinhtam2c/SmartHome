using Core.Domain.Devices;

namespace Application.Common.Capabilities;

public interface ICapabilityRegistry
{
    bool TryGetDefinition(string capabilityId, int version, out CapabilityDefinition definition);

    CapabilityDefinition GetRequiredDefinition(string capabilityId, int version);

    IReadOnlyCollection<CapabilityDefinition> GetAll();
}
