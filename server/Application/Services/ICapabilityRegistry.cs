using Core.Domain.Devices;

namespace Application.Services;

public interface ICapabilityRegistry
{
    bool TryGetDefinition(string capabilityId, int version, out CapabilityDefinition definition);

    CapabilityDefinition GetRequiredDefinition(string capabilityId, int version);

    IReadOnlyCollection<CapabilityDefinition> GetAll();
}

public sealed class CapabilityRegistryFileOptions
{
    public string FilePath { get; set; } = "capability-registry.json";
}
