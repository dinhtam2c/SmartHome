using Application.BusinessServices.Devices.Realtime;
using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.Events;

internal sealed class DeviceDeletedDomainEventHandler : INotificationHandler<DeviceDeletedDomainEvent>
{
    private readonly IDeviceRealtimeNotifier _realtimeDeltaNotifier;
    public DeviceDeletedDomainEventHandler(IDeviceRealtimeNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public async Task Handle(DeviceDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _realtimeDeltaNotifier.PublishDeleted(
            notification.DeviceId,
            notification.HomeId,
            notification.RoomId,
            new { notification.IsOnline },
            cancellationToken);
    }
}
