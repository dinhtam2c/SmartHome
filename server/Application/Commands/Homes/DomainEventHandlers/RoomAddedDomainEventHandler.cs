using Application.Common.Realtime;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.DomainEventHandlers;

internal sealed class RoomAddedDomainEventHandler : INotificationHandler<RoomAddedDomainEvent>
{
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;

    public RoomAddedDomainEventHandler(IRealtimeDeltaNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(RoomAddedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishRoomDelta(
            notification.HomeId,
            notification.RoomId,
            RealtimeChanges.Created,
            new
            {
                Id = notification.RoomId,
                notification.Name,
                notification.Description,
                DeviceCount = 0,
                OnlineDeviceCount = 0,
                Temperature = (double?)null,
                Humidity = (double?)null
            },
            cancellationToken);
    }
}
