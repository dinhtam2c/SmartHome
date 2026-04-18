using Application.Common.Realtime;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.DomainEventHandlers;

internal sealed class RoomUpdatedDomainEventHandler : INotificationHandler<RoomUpdatedDomainEvent>
{
    private readonly IRealtimeDetailsNotifier _realtimeDetailsNotifier;

    public RoomUpdatedDomainEventHandler(IRealtimeDetailsNotifier realtimeDetailsNotifier)
    {
        _realtimeDetailsNotifier = realtimeDetailsNotifier;
    }

    public Task Handle(RoomUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDetailsNotifier.PublishRoomDetailsChanged(
            notification.HomeId,
            notification.RoomId,
            cancellationToken);
    }
}
