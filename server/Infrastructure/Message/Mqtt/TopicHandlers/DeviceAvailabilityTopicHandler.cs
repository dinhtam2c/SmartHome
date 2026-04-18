using Application.Commands.Devices.UpdateDeviceAvailability;
using Infrastructure.Message.Mqtt.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Message.Mqtt.TopicHandlers;

public sealed class DeviceAvailabilityTopicHandler : MqttMediatRTopicHandler<DeviceAvailabilityMessage>
{
    public DeviceAvailabilityTopicHandler(
        ISender sender,
        ILogger<DeviceAvailabilityTopicHandler> logger
    )
        : base(sender, logger)
    {
    }

    protected override object MapToRequest(
        MqttRouteContext routeContext,
        DeviceAvailabilityMessage message
    )
    {
        var deviceId = Guid.Parse(routeContext.GetRequired("deviceId"));

        return new UpdateDeviceAvailabilityCommand(deviceId, message.State);
    }
}
