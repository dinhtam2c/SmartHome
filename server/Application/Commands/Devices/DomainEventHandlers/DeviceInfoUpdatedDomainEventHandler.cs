using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceInfoUpdatedDomainEventHandler : INotificationHandler<DeviceInfoUpdatedDomainEvent>
{
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;

    public DeviceInfoUpdatedDomainEventHandler(IRealtimeDeltaNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(DeviceInfoUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDeviceDelta(
            notification.DeviceId,
            notification.HomeId,
            notification.RoomId,
            RealtimeChanges.Updated,
            new { notification.Name },
            cancellationToken: cancellationToken);
    }
}
