using Application.Common.Message;
using Application.DTOs.ProvisionDto;
using Application.Exceptions;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.MessageHandlers;

public class DeviceProvisionHandler : IMessageHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GatewayProvisionHandler> _logger;

    public string TopicPattern => MessageTopics.DeviceProvision;

    public Type MessageType => typeof(DeviceProvisionRequest);

    public DeviceProvisionHandler(IServiceScopeFactory scopeFactory, ILogger<GatewayProvisionHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleMessage(string topic, object message)
    {
        var request = (DeviceProvisionRequest)message;
        using var scope = _scopeFactory.CreateScope();
        var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();

        await deviceService.DeviceProvision(request);
    }
}
