using Application.Common.Message;
using Application.DTOs.ProvisionDto;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.MessageHandlers;

public class GatewayProvisionHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GatewayProvisionHandler> _logger;

    public string TopicPattern => MessageTopics.GatewayProvision;

    public Type MessageType => typeof(GatewayProvisionRequest);

    public GatewayProvisionHandler(IServiceScopeFactory scopeFactory, ILogger<GatewayProvisionHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleMessage(string topic, object message)
    {
        var request = (GatewayProvisionRequest)message;
        using var scope = _scopeFactory.CreateScope();
        var gatewayService = scope.ServiceProvider.GetRequiredService<IGatewayService>();

        await gatewayService.GatewayProvision(request);
    }
}
