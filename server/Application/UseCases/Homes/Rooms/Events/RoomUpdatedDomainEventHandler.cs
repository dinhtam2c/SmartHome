using Application.BusinessServices.Homes.RoomsRealtime;
using Application.Common.Realtime;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Homes.Rooms.Events;

internal sealed class RoomUpdatedDomainEventHandler : INotificationHandler<RoomUpdatedDomainEvent>
{
    private readonly IRoomRealtimeNotifier _realtimeDeltaNotifier;

    public RoomUpdatedDomainEventHandler(IRoomRealtimeNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(RoomUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDelta(
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
