using Application.Common.Realtime;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.DomainEventHandlers;

internal sealed class RoomDeletedDomainEventHandler : INotificationHandler<RoomDeletedDomainEvent>
{
    private readonly IRealtimeDetailsNotifier _realtimeDetailsNotifier;

    public RoomDeletedDomainEventHandler(IRealtimeDetailsNotifier realtimeDetailsNotifier)
    {
        _realtimeDetailsNotifier = realtimeDetailsNotifier;
    }

    public Task Handle(RoomDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDetailsNotifier.PublishRoomDeleted(
            notification.HomeId,
            notification.RoomId,
            cancellationToken);
    }
}
