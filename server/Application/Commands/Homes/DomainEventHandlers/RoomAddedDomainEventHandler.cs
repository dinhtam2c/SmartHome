using Application.Common.Realtime;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.DomainEventHandlers;

internal sealed class RoomAddedDomainEventHandler : INotificationHandler<RoomAddedDomainEvent>
{
    private readonly IRealtimeDetailsNotifier _realtimeDetailsNotifier;

    public RoomAddedDomainEventHandler(IRealtimeDetailsNotifier realtimeDetailsNotifier)
    {
        _realtimeDetailsNotifier = realtimeDetailsNotifier;
    }

    public Task Handle(RoomAddedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDetailsNotifier.PublishRoomDetailsChanged(
            notification.HomeId,
            notification.RoomId,
            cancellationToken);
    }
}
