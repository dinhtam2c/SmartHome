using Application.BusinessServices.Devices.Realtime;
using Application.Common.Realtime;
using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.Events;

internal sealed class DeviceInfoUpdatedDomainEventHandler : INotificationHandler<DeviceInfoUpdatedDomainEvent>
{
    private readonly IDeviceRealtimeNotifier _realtimeDeltaNotifier;

    public DeviceInfoUpdatedDomainEventHandler(IDeviceRealtimeNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(DeviceInfoUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDelta(
            notification.DeviceId,
            notification.HomeId,
            notification.RoomId,
            RealtimeChanges.Updated,
            new { notification.Name },
            cancellationToken: cancellationToken);
    }
}
