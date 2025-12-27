using Application.Common.Message;
using Application.DTOs.Messages.Devices;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.MessageHandlers;

public class DeviceAvailabilityHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    public string TopicPattern => MessageTopics.DeviceAvailability;

    public Type MessageType => typeof(DeviceAvailability);

    public DeviceAvailabilityHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleMessage(string[] topicTokens, object message)
    {
        var gatewayId = Guid.Parse(topicTokens[2]);
        var deviceId = Guid.Parse(topicTokens[4]);
        var availability = (DeviceAvailability)message;

        var deviceService = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IDeviceService>();
        await deviceService.HandleDeviceAvailability(gatewayId, deviceId, availability);
    }
}
