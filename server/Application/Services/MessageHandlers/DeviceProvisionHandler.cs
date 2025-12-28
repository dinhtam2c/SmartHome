using Application.Common.Message;
using Application.DTOs.Messages.Provision;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.MessageHandlers;

public class DeviceProvisionHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string TopicPattern => MessageTopics.DeviceProvision;

    public Type MessageType => typeof(DeviceProvisionRequest);

    public DeviceProvisionHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleMessage(string[] topicTokens, object message)
    {
        var gatewayId = Guid.Parse(topicTokens[2]);
        var request = (DeviceProvisionRequest)message;

        using var scope = _scopeFactory.CreateScope();
        var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();

        await deviceService.DeviceProvision(gatewayId, request);
    }
}
