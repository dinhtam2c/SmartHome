namespace Core.Domain.Data;

public interface IDeviceCapabilityStateHistoryRepository
{
    Task Add(DeviceCapabilityStateHistory history);

    Task<IEnumerable<DeviceCapabilityStateHistory>> GetByCapability(
        Guid deviceId,
        string capabilityId,
        string endpointId,
        long? from,
        long? to,
        int limit);
}
