using Application.Commands.Devices.ProvisionDevice;
using Infrastructure.Message.Mqtt.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Message.Mqtt.TopicHandlers;

public sealed class DeviceProvisionTopicHandler : MqttMediatRTopicHandler<ProvisionDeviceMessage>
{
    public DeviceProvisionTopicHandler(
        ISender sender,
        ILogger<DeviceProvisionTopicHandler> logger
    )
        : base(sender, logger)
    {
    }

    protected override object MapToRequest(
        MqttRouteContext routeContext,
        ProvisionDeviceMessage message
    )
    {
        var macAddress = routeContext.GetRequired("macAddress");

        return new ProvisionDeviceCommand(
            message.Name,
            macAddress,
            message.FirmwareVersion,
            message.Protocol,
            message.Endpoints.Select(endpoint => new DeviceEndpointModel(
                endpoint.EndpointId,
                endpoint.Name,
                endpoint.Capabilities.Select(capability => new DeviceCapabilityModel(
                    capability.CapabilityId,
                    capability.CapabilityVersion,
                    capability.SupportedOperations
                ))))
        );
    }
}
