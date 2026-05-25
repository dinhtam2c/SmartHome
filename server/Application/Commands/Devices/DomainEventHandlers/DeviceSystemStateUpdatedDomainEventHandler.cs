using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceSystemStateUpdatedDomainEventHandler : INotificationHandler<DeviceSystemStateUpdatedDomainEvent>
{
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;

    public DeviceSystemStateUpdatedDomainEventHandler(IRealtimeDeltaNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(DeviceSystemStateUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDeviceDelta(
            notification.DeviceId,
            notification.HomeId,
            notification.RoomId,
            RealtimeChanges.Updated,
            new
            {
                notification.Uptime,
                notification.LastSeenAt
            },
            cancellationToken: cancellationToken);
    }
}
