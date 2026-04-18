using Application.Common.Message;
using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceProvisionedDomainEventHandler : INotificationHandler<DeviceProvisionedDomainEvent>
{
    private readonly IDeviceMessagePublisher _deviceMessagePublisher;
    private readonly IRealtimeDetailsNotifier _realtimeDetailsNotifier;

    public DeviceProvisionedDomainEventHandler(
        IDeviceMessagePublisher deviceMessagePublisher,
        IRealtimeDetailsNotifier realtimeDetailsNotifier)
    {
        _deviceMessagePublisher = deviceMessagePublisher;
        _realtimeDetailsNotifier = realtimeDetailsNotifier;
    }

    public async Task Handle(DeviceProvisionedDomainEvent notification, CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            _deviceMessagePublisher.SendCredentials(
                notification.MacAddress,
                notification.DeviceId,
                notification.AccessToken),
            _realtimeDetailsNotifier.PublishDeviceDetailsChanged(notification.DeviceId, cancellationToken));
    }
}
