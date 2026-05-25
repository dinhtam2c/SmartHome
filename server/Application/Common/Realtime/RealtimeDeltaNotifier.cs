namespace Application.Common.Realtime;

public sealed class RealtimeDeltaNotifier : IRealtimeDeltaNotifier
{
    private readonly IRealtimePublisher _realtimePublisher;

    public RealtimeDeltaNotifier(IRealtimePublisher realtimePublisher)
    {
        _realtimePublisher = realtimePublisher;
    }

    public async Task PublishDeviceDelta(
        Guid deviceId,
        Guid? homeId,
        Guid? roomId,
        string change,
        object? delta,
        Guid? previousRoomId = null,
        CancellationToken cancellationToken = default)
    {
        var realtimeDelta = RealtimeDelta.Create(
            entity: RealtimeEntities.Device,
            change: change,
            homeId: homeId,
            roomId: roomId,
            previousRoomId: previousRoomId,
            deviceId: deviceId,
            delta: delta);

        await _realtimePublisher.PublishToDevice(deviceId, realtimeDelta, cancellationToken);

        if (roomId.HasValue)
        {
            await _realtimePublisher.PublishToRoom(roomId.Value, realtimeDelta, cancellationToken);
        }

        if (previousRoomId.HasValue && previousRoomId != roomId)
        {
            await _realtimePublisher.PublishToRoom(previousRoomId.Value, realtimeDelta, cancellationToken);
        }

        if (homeId.HasValue)
        {
            await _realtimePublisher.PublishToHome(homeId.Value, realtimeDelta, cancellationToken);
        }
    }

    public async Task PublishDeviceDeleted(
        Guid deviceId,
        Guid? homeId,
        Guid? roomId,
        object? delta = null,
        CancellationToken cancellationToken = default)
    {
        var realtimeDelta = RealtimeDelta.Create(
            entity: RealtimeEntities.Device,
            change: RealtimeChanges.Deleted,
            homeId: homeId,
            roomId: roomId,
            deviceId: deviceId,
            delta: delta);

        await _realtimePublisher.PublishToDevice(deviceId, realtimeDelta, cancellationToken);

        if (roomId.HasValue)
        {
            await _realtimePublisher.PublishToRoom(roomId.Value, realtimeDelta, cancellationToken);
        }

        if (homeId.HasValue)
        {
            await _realtimePublisher.PublishToHome(homeId.Value, realtimeDelta, cancellationToken);
        }
    }

    public async Task PublishRoomDelta(
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

        await _realtimePublisher.PublishToRoom(roomId, realtimeDelta, cancellationToken);
        await _realtimePublisher.PublishToHome(homeId, realtimeDelta, cancellationToken);
    }

    public async Task PublishRoomDeleted(
        Guid homeId,
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var delta = RealtimeDelta.Create(
            entity: RealtimeEntities.Room,
            change: RealtimeChanges.Deleted,
            homeId: homeId,
            roomId: roomId);

        await _realtimePublisher.PublishToRoom(roomId, delta, cancellationToken);
        await _realtimePublisher.PublishToHome(homeId, delta, cancellationToken);
    }
}
