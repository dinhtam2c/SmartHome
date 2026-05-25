using Application.BusinessServices.Homes.RoomsRealtime;
using Application.Common.Realtime;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Homes.Rooms.Events;

internal sealed class RoomAddedDomainEventHandler : INotificationHandler<RoomAddedDomainEvent>
{
    private readonly IRoomRealtimeNotifier _realtimeDeltaNotifier;

    public RoomAddedDomainEventHandler(IRoomRealtimeNotifier realtimeDeltaNotifier)
    {
        _realtimeDeltaNotifier = realtimeDeltaNotifier;
    }

    public Task Handle(RoomAddedDomainEvent notification, CancellationToken cancellationToken)
    {
        return _realtimeDeltaNotifier.PublishDelta(
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
