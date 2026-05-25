using Application.Commands.Devices.SendDeviceCommand;

namespace Application.Common.Message;

public interface IDeviceMessagePublisher
{
    Task SendProvisionCode(string macAddress, string provisionCode);
    Task SendCredentials(string macAddress, Guid deviceId, string accessToken);
    Task SendCommand(DeviceCommandModel command);
}
