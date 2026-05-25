using Application.Common.Realtime;
using Application.Ports.Realtime;

namespace Application.BusinessServices.Homes.RoomsRealtime;

public sealed class RoomRealtimeNotifier : IRoomRealtimeNotifier
{
    private readonly IRealtimePublisher _publisher;

    public RoomRealtimeNotifier(IRealtimePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishDelta(
        Guid homeId,
        Guid roomId,
        string change,
        object? delta,
        CancellationToken cancellationToken = default)
    {
        var realtimeDelta = RealtimeDelta.Create(
            entity: RealtimeEntities.Room,
            change: change,
            homeId: homeId,
            roomId: roomId,
            delta: delta);

        await _publisher.PublishToRoom(roomId, realtimeDelta, cancellationToken);
        await _publisher.PublishToHome(homeId, realtimeDelta, cancellationToken);
    }

    public async Task PublishDeleted(
        Guid homeId,
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var delta = RealtimeDelta.Create(
            entity: RealtimeEntities.Room,
            change: RealtimeChanges.Deleted,
            homeId: homeId,
            roomId: roomId);

        await _publisher.PublishToRoom(roomId, delta, cancellationToken);
        await _publisher.PublishToHome(homeId, delta, cancellationToken);
    }
}
