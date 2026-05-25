using Application.BusinessServices.Devices.Realtime;
using Application.Common.Realtime;
using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.Events;

internal sealed class DeviceSystemStateUpdatedDomainEventHandler : INotificationHandler<DeviceSystemStateUpdatedDomainEvent>
{
    private readonly IDeviceRealtimeNotifier _realtimeDeltaNotifier;

    public DeviceSystemStateUpdatedDomainEventHandler(IDeviceRealtimeNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(DeviceSystemStateUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDelta(
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
