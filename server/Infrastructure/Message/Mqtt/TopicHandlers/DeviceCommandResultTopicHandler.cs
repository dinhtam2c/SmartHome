using Application.Commands.Devices.UpdateDeviceCommandResult;
using Infrastructure.Message.Mqtt.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Message.Mqtt.TopicHandlers;

public sealed class DeviceCommandResultTopicHandler : MqttMediatRTopicHandler<DeviceCommandResultMessage>
{
    public DeviceCommandResultTopicHandler(
        ISender sender,
        ILogger<DeviceCommandResultTopicHandler> logger
    )
        : base(sender, logger)
    {
    }

    protected override object MapToRequest(
        MqttRouteContext routeContext,
        DeviceCommandResultMessage message
    )
    {
        var deviceId = Guid.Parse(routeContext.GetRequired("deviceId"));

        return new UpdateDeviceCommandResultCommand(
            deviceId,
            message.CapabilityId,
            message.CorrelationId,
            message.Operation,
            message.Status,
            message.Value,
            message.Error
        );
    }
}
