using Application.BusinessServices.Devices.Realtime;
using Application.Common.Realtime;
using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.Events;

internal sealed class DeviceWentOnlineDomainEventHandler : INotificationHandler<DeviceWentOnlineDomainEvent>
{
    private readonly IDeviceRealtimeNotifier _realtimeDeltaNotifier;

    public DeviceWentOnlineDomainEventHandler(IDeviceRealtimeNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(DeviceWentOnlineDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDelta(
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
