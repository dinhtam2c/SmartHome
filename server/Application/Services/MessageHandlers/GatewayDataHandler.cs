using Application.Common.Message;
using Application.DTOs.SensorDataDto;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.MessageHandlers;

public class GatewayDataHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string TopicPattern => MessageTopics.DeviceData;

    public Type MessageType => typeof(GatewayData);

    public GatewayDataHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task HandleMessage(string[] topicTokens, object message)
    {
        var gatewayId = Guid.Parse(topicTokens[2]);
        var gatewayData = (GatewayData)message;

        using var scope = _scopeFactory.CreateScope();
        var sensorDataService = scope.ServiceProvider.GetRequiredService<ISensorDataService>();

        await sensorDataService.StoreSensorData(gatewayId, gatewayData);
    }
}
