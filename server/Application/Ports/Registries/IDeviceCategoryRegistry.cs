using Domain.Models.DeviceCategories;

namespace Application.Ports.Registries;

public interface IDeviceCategoryRegistry
{
    bool TryGetDefinition(string categoryId, out DeviceCategoryDefinition definition);

    DeviceCategoryDefinition GetRequiredDefinition(string categoryId);

    IReadOnlyCollection<DeviceCategoryDefinition> GetAll();
}
