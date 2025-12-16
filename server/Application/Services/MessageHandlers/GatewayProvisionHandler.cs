using Application.Common.Message;
using Application.DTOs.ProvisionDto;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.MessageHandlers;

public class GatewayProvisionHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string TopicPattern => MessageTopics.GatewayProvision;

    public Type MessageType => typeof(GatewayProvisionRequest);

    public GatewayProvisionHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleMessage(string[] topicTokens, object message)
    {
        var request = (GatewayProvisionRequest)message;
        using var scope = _scopeFactory.CreateScope();
        var gatewayService = scope.ServiceProvider.GetRequiredService<IGatewayService>();

        await gatewayService.GatewayProvision(request);
    }
}
