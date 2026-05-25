using Core.Domain.Devices;

namespace Application.Common.DeviceCategories;

public interface IDeviceCategoryRegistry
{
    bool TryGetDefinition(string categoryId, out DeviceCategoryDefinition definition);

    DeviceCategoryDefinition GetRequiredDefinition(string categoryId);

    IReadOnlyCollection<DeviceCategoryDefinition> GetAll();
}
