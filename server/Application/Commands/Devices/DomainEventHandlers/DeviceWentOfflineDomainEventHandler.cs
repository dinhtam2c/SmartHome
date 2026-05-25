using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceWentOfflineDomainEventHandler : INotificationHandler<DeviceWentOfflineDomainEvent>
{
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;

    public DeviceWentOfflineDomainEventHandler(IRealtimeDeltaNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(DeviceWentOfflineDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDeviceDelta(
            notification.DeviceId,
            notification.HomeId,
            notification.RoomId,
            RealtimeChanges.StatusChanged,
            new
            {
                IsOnline = false,
                Uptime = 0,
                notification.LastSeenAt
            },
            cancellationToken: cancellationToken);
    }
}
