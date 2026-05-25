using Application.BusinessServices.Homes.RoomsRealtime;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Homes.Rooms.Events;

internal sealed class RoomDeletedDomainEventHandler : INotificationHandler<RoomDeletedDomainEvent>
{
    private readonly IRoomRealtimeNotifier _realtimeDeltaNotifier;
    public RoomDeletedDomainEventHandler(IRoomRealtimeNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public async Task Handle(RoomDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _realtimeDeltaNotifier.PublishDeleted(
            notification.HomeId,
            notification.RoomId,
            cancellationToken);
    }
}
