using Application.Common.Realtime;
using Application.Ports.Realtime;
using Domain.Models.Floors;
using MediatR;

namespace Application.UseCases.Floors.Events;

internal sealed class FloorChangedDomainEventHandler
    : INotificationHandler<FloorChangedDomainEvent>
{
    private readonly IRealtimePublisher _realtimePublisher;

    public FloorChangedDomainEventHandler(IRealtimePublisher realtimePublisher)
    {
        _realtimePublisher = realtimePublisher;
    }

    public Task Handle(FloorChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var listChanged = notification.Reason is FloorChangeReasons.Created
            or FloorChangeReasons.Deleted
            or FloorChangeReasons.InfoUpdated;

        return _realtimePublisher.PublishToHome(
            notification.HomeId,
            RealtimeDelta.Create(
                entity: RealtimeEntities.Floor,
                change: notification.Reason == FloorChangeReasons.Created
                    ? RealtimeChanges.Created
                    : notification.Reason == FloorChangeReasons.Deleted
                        ? RealtimeChanges.Deleted
                        : RealtimeChanges.Updated,
                homeId: notification.HomeId,
                floorId: notification.FloorId,
                delta: new
                {
                    notification.Reason,
                    ListChanged = listChanged
                }),
            cancellationToken);
    }
}
