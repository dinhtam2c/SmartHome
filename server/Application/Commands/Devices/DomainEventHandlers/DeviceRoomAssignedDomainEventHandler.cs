using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceRoomAssignedDomainEventHandler : INotificationHandler<DeviceRoomAssignedDomainEvent>
{
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;

    public DeviceRoomAssignedDomainEventHandler(IRealtimeDeltaNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public async Task Handle(DeviceRoomAssignedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _realtimeDeltaNotifier.PublishDeviceDelta(
            notification.DeviceId,
            notification.HomeId,
            notification.RoomId,
            RealtimeChanges.Moved,
            new
            {
                notification.RoomId,
                notification.PreviousRoomId
            },
            previousRoomId: notification.PreviousRoomId,
            cancellationToken: cancellationToken);
    }
}
