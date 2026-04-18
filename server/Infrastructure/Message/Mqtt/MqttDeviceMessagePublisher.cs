using Application.Commands.Devices.SendDeviceCommand;
using Application.Common.Message;
using Infrastructure.Message.Mqtt.Dtos;

namespace Infrastructure.Message.Mqtt;

public class MqttDeviceMessagePublisher : IDeviceMessagePublisher
{
    private readonly MqttService _mqttService;

    public MqttDeviceMessagePublisher(MqttService mqttService)
    {
        _mqttService = mqttService;
    }

    public async Task SendCommand(DeviceCommandModel command)
    {
        var topic = MqttTopics.DeviceCommand(command.DeviceId);
        var message = DeviceCommandMessage.New(
            command.EndpointId,
            command.CapabilityId,
            command.Operation,
            command.Value,
            command.CorrelationId);
        await _mqttService.PublishMessage(topic, message, 2, false);
    }

    public async Task SendCredentials(string macAddress, Guid deviceId, string accessToken)
    {
        var topic = MqttTopics.DeviceProvisionResponse(macAddress);
        var message = new DeviceCredentialsResponseMessage(deviceId, accessToken);
        await _mqttService.PublishMessage(topic, message, 1, false);
    }

    public async Task SendProvisionCode(string macAddress, string provisionCode)
    {
        var topic = MqttTopics.DeviceProvisionResponse(macAddress);
        var message = new DeviceProvisionResponseMessage(provisionCode);
        await _mqttService.PublishMessage(topic, message, 1, false);
    }
}
