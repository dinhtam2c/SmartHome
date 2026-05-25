using Application.Common.Realtime;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.DomainEventHandlers;

internal sealed class RoomUpdatedDomainEventHandler : INotificationHandler<RoomUpdatedDomainEvent>
{
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;

    public RoomUpdatedDomainEventHandler(IRealtimeDeltaNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(RoomUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishRoomDelta(
            notification.HomeId,
            notification.RoomId,
            RealtimeChanges.Updated,
            new
            {
                notification.Name,
                notification.Description
            },
            cancellationToken);
    }
}
