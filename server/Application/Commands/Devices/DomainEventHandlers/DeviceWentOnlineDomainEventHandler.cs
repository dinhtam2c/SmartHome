using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceWentOnlineDomainEventHandler : INotificationHandler<DeviceWentOnlineDomainEvent>
{
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;

    public DeviceWentOnlineDomainEventHandler(IRealtimeDeltaNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(DeviceWentOnlineDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDeviceDelta(
            notification.DeviceId,
            notification.HomeId,
            notification.RoomId,
            RealtimeChanges.StatusChanged,
            new
            {
                IsOnline = true,
                notification.LastSeenAt
            },
            cancellationToken: cancellationToken);
    }
}
