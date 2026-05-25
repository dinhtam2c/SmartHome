using Application.BusinessServices.Devices.Realtime;
using Application.Common.Realtime;
using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.Events;

internal sealed class DeviceRoomAssignedDomainEventHandler : INotificationHandler<DeviceRoomAssignedDomainEvent>
{
    private readonly IDeviceRealtimeNotifier _realtimeDeltaNotifier;

    public DeviceRoomAssignedDomainEventHandler(IDeviceRealtimeNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public async Task Handle(DeviceRoomAssignedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _realtimeDeltaNotifier.PublishDelta(
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
