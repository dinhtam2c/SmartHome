using Application.UseCases.Devices.State.ApplySystemStateReport;
using Infrastructure.Message.Mqtt.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Message.Mqtt.TopicHandlers;

public sealed class DeviceSystemStateTopicHandler : MqttMediatRTopicHandler<DeviceSystemStateMessage>
{
    public DeviceSystemStateTopicHandler(
        ISender sender,
        ILogger<DeviceSystemStateTopicHandler> logger
    )
        : base(sender, logger)
    {
    }

    protected override object MapToRequest(
        MqttRouteContext routeContext,
        DeviceSystemStateMessage message
    )
    {
        var deviceId = Guid.Parse(routeContext.GetRequired("deviceId"));

        return new ApplyDeviceSystemStateReport(deviceId, message.Uptime);
    }
}
