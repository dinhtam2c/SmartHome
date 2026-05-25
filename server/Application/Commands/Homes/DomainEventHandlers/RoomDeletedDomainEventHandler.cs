using Application.Common.Realtime;
using Core.Domain.Floors;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.DomainEventHandlers;

internal sealed class RoomDeletedDomainEventHandler : INotificationHandler<RoomDeletedDomainEvent>
{
    private readonly IRealtimeDeltaNotifier _realtimeDeltaNotifier;
    private readonly IRealtimePublisher _realtimePublisher;

    public RoomDeletedDomainEventHandler(
        IRealtimeDeltaNotifier realtimeDeltaNotifier,
        IRealtimePublisher realtimePublisher)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
        _realtimePublisher = realtimePublisher;
    }

    public async Task Handle(RoomDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _realtimeDeltaNotifier.PublishRoomDeleted(
            notification.HomeId,
            notification.RoomId,
            cancellationToken);

        await _realtimePublisher.PublishToHome(
            notification.HomeId,
            RealtimeDelta.Create(
                entity: RealtimeEntities.Floor,
                change: RealtimeChanges.Updated,
                homeId: notification.HomeId,
                roomId: notification.RoomId,
                delta: new { Reason = FloorChangeReasons.LinkedRoomDeleted }),
            cancellationToken);
    }
}
