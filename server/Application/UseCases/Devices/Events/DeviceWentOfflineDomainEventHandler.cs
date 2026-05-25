using Application.BusinessServices.Devices.Realtime;
using Application.Common.Realtime;
using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.Events;

internal sealed class DeviceWentOfflineDomainEventHandler : INotificationHandler<DeviceWentOfflineDomainEvent>
{
    private readonly IDeviceRealtimeNotifier _realtimeDeltaNotifier;

    public DeviceWentOfflineDomainEventHandler(IDeviceRealtimeNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(DeviceWentOfflineDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDelta(
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
