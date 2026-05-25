namespace Application.Ports.Messages;

public interface IDeviceAccessManager
{
    Task UpsertDeviceAccess(
        Guid deviceId,
        string password,
        CancellationToken cancellationToken);

    Task DeleteDeviceAccess(
        Guid deviceId,
        CancellationToken cancellationToken);
}
