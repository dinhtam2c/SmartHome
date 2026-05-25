using Application.Common.Realtime;
using Core.Domain.Devices;
using Core.Domain.Floors;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceDeletedDomainEventHandler : INotificationHandler<DeviceDeletedDomainEvent>
{
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;
    private readonly IRealtimePublisher _realtimePublisher;

    public DeviceDeletedDomainEventHandler(
        IRealtimeDeltaNotifier realtimeDeltaNotifier,
        IRealtimePublisher realtimePublisher)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
        _realtimePublisher = realtimePublisher;
    }

    public async Task Handle(DeviceDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _realtimeDeltaNotifier.PublishDeviceDeleted(
            notification.DeviceId,
            notification.HomeId,
            notification.RoomId,
            new { notification.IsOnline },
            cancellationToken);

        if (notification.HomeId.HasValue)
        {
            await _realtimePublisher.PublishToHome(
                notification.HomeId.Value,
                RealtimeDelta.Create(
                    entity: RealtimeEntities.Floor,
                    change: RealtimeChanges.Updated,
                    homeId: notification.HomeId.Value,
                    deviceId: notification.DeviceId,
                    delta: new { Reason = FloorChangeReasons.DeviceDeleted }),
                cancellationToken);
        }
    }
}
