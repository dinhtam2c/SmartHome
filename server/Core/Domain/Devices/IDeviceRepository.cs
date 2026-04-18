namespace Core.Domain.Devices;

public interface IDeviceRepository
{
    Task<Device?> GetById(Guid id, CancellationToken ct = default);
    Task<Device?> GetByMacAddress(string macAddress, CancellationToken ct = default);
    Task<Device?> GetByProvisionCode(string provisionCode, CancellationToken ct = default);
    Task Add(Device device, CancellationToken ct = default);
    void Remove(Device device);
}
