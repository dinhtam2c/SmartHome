using Application.Common.Realtime;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class DeviceRoomAssignedDomainEventHandler : INotificationHandler<DeviceRoomAssignedDomainEvent>
{
    private readonly IRealtimeDetailsNotifier _realtimeDetailsNotifier;

    public DeviceRoomAssignedDomainEventHandler(IRealtimeDetailsNotifier realtimeDetailsNotifier)
    {
        _realtimeDetailsNotifier = realtimeDetailsNotifier;
    }

    public async Task Handle(DeviceRoomAssignedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _realtimeDetailsNotifier.PublishDeviceDetailsChanged(notification.DeviceId, cancellationToken);

        if (notification.PreviousRoomId.HasValue &&
            notification.PreviousRoomId != notification.RoomId)
        {
            await _realtimeDetailsNotifier.PublishRoomDetailsChanged(
                notification.HomeId,
                notification.PreviousRoomId.Value,
                cancellationToken);
        }
    }
}
