using Application.Common.Message;
using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceProvisionedDomainEventHandler : INotificationHandler<DeviceProvisionedDomainEvent>
{
    private readonly IDeviceMessagePublisher _deviceMessagePublisher;
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;

    public DeviceProvisionedDomainEventHandler(
        IDeviceMessagePublisher deviceMessagePublisher,
        IRealtimeDeltaNotifier realtimeDeltaNotifier)
    {
        _deviceMessagePublisher = deviceMessagePublisher;
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public async Task Handle(DeviceProvisionedDomainEvent notification, CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            _deviceMessagePublisher.SendCredentials(
                notification.MacAddress,
                notification.DeviceId,
                notification.AccessToken),
            _realtimeDeltaNotifier.PublishDeviceDelta(
                notification.DeviceId,
                notification.HomeId,
                notification.RoomId,
                RealtimeChanges.Created,
                new
                {
                    notification.HomeId,
                    notification.RoomId
                },
                cancellationToken: cancellationToken));
    }
}
