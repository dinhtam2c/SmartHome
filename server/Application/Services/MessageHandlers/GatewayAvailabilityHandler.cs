using Application.Common.Message;
using Application.DTOs.Messages.Gateways;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.MessageHandlers;

public class GatewayAvailabilityHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    public string TopicPattern => MessageTopics.GatewayAvailability;

    public Type MessageType => typeof(GatewayAvailability);

    public GatewayAvailabilityHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleMessage(string[] topicTokens, object message)
    {
        var gatewayId = Guid.Parse(topicTokens[2]);
        var availability = (GatewayAvailability)message;

        var gatewayService = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IGatewayService>();
        await gatewayService.HandleGatewayAvailability(gatewayId, availability);
    }
}
