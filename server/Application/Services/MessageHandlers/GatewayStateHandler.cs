using Application.Common.Message;
using Application.DTOs.GatewayDto;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.MessageHandlers;

public class GatewayStateHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    public string TopicPattern => MessageTopics.GatewayState;

    public Type MessageType => typeof(GatewayState);

    public GatewayStateHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleMessage(string[] topicTokens, object message)
    {
        var gatewayId = Guid.Parse(topicTokens[2]);
        var state = (GatewayState)message;

        var gatewayService = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IGatewayService>();
        await gatewayService.HandleGatewayState(gatewayId, state);
    }
}
