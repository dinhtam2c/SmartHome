namespace Application.Ports.Messages;

public interface IDeviceCommandSender
{
    Task Send(DeviceCommandRequest command, CancellationToken cancellationToken);
}
