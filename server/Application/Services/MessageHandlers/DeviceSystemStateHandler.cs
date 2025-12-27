using Application.Common.Message;
using Application.DTOs.DeviceDto;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.MessageHandlers;

public class DeviceSystemStateHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    public string TopicPattern => MessageTopics.DeviceSystemState;

    public Type MessageType => typeof(DeviceSystemState);

    public DeviceSystemStateHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleMessage(string[] topicTokens, object message)
    {
        var gatewayId = Guid.Parse(topicTokens[2]);
        var deviceId = Guid.Parse(topicTokens[4]);
        var state = (DeviceSystemState)message;

        var deviceService = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IDeviceService>();
        await deviceService.HandleDeviceSystemState(gatewayId, deviceId, state);
    }
}
