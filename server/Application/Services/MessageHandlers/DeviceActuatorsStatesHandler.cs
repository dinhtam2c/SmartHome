using Application.Common.Message;
using Application.DTOs.DeviceDto;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.MessageHandlers;

public class DeviceActuatorsStatesHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    public string TopicPattern => MessageTopics.DeviceActuatorsStates;

    public Type MessageType => typeof(IEnumerable<DeviceActuatorStates>);

    public DeviceActuatorsStatesHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleMessage(string[] topicTokens, object message)
    {
        var gatewayId = Guid.Parse(topicTokens[2]);
        var deviceId = Guid.Parse(topicTokens[4]);
        var actuatorsStates = (IEnumerable<DeviceActuatorStates>)message;

        var deviceService = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IDeviceService>();
        await deviceService.HandleDeviceActuatorsStates(gatewayId, deviceId, actuatorsStates);
    }
}
