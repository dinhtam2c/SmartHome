using Application.Common.Message;
using Application.DTOs.SensorDataDto;
using Application.Exceptions;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.MessageHandlers;

public class GatewayDataHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GatewayDataHandler> _logger;

    public string TopicPattern => MessageTopics.DeviceData;

    public Type MessageType => typeof(GatewayData);

    public GatewayDataHandler(IServiceScopeFactory scopeFactory, ILogger<GatewayDataHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleMessage(string topic, object message)
    {
        var gatewayData = (GatewayData)message;

        using var scope = _scopeFactory.CreateScope();
        var sensorDataService = scope.ServiceProvider.GetRequiredService<ISensorDataService>();

        await sensorDataService.StoreSensorData(gatewayData);
    }
}
