using Domain.Models.Capabilities;

namespace Application.Ports.Registries;

public interface ICapabilityRegistry
{
    bool TryGetDefinition(string capabilityId, int version, out CapabilityDefinition definition);

    CapabilityDefinition GetRequiredDefinition(string capabilityId, int version);

    IReadOnlyCollection<CapabilityDefinition> GetAll();
}
