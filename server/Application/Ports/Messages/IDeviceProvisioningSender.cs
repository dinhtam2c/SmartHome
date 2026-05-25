namespace Application.Ports.Messages;

public interface IDeviceProvisioningSender
{
    Task SendProvisionCode(
        string macAddress,
        string provisionCode,
        CancellationToken cancellationToken);

    Task SendCredentials(
        string macAddress,
        Guid deviceId,
        string accessToken,
        CancellationToken cancellationToken);
}
