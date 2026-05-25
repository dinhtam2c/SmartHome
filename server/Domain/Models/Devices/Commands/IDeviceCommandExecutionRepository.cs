namespace Domain.Models.Devices.Commands;

public interface IDeviceCommandExecutionRepository
{
    Task Add(DeviceCommandExecution execution);

    Task<DeviceCommandExecution?> GetByCorrelation(Guid deviceId, string correlationId);

    Task<IEnumerable<DeviceCommandExecution>> GetPendingOlderThan(long unixCutoff, int limit);
}
