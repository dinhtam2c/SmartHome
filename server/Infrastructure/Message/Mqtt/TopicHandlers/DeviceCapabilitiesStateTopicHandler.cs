using Application.UseCases.Devices.State.ApplyStateReport;
using Application.BusinessServices.Devices.State;
using Infrastructure.Message.Mqtt.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Message.Mqtt.TopicHandlers;

public sealed class DeviceCapabilitiesStateTopicHandler
    : MqttMediatRTopicHandler<IEnumerable<DeviceCapabilityStateMessage>>
{
    public DeviceCapabilitiesStateTopicHandler(
        ISender sender,
        ILogger<DeviceCapabilitiesStateTopicHandler> logger
    )
        : base(sender, logger)
    {
    }

    protected override object MapToRequest(
        MqttRouteContext routeContext,
        IEnumerable<DeviceCapabilityStateMessage> message
    )
    {
        var deviceId = Guid.Parse(routeContext.GetRequired("deviceId"));

        return new ApplyCapabilityStateReport(
            deviceId,
            message.Select(
                state => new CapabilityStateUpdate(
                    state.CapabilityId,
                    state.EndpointId,
                    state.State
                )).ToList()
        );
    }
}
