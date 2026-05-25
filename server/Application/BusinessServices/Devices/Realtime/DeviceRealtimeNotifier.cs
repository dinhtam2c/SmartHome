using Application.Common.Realtime;
using Application.Ports.Realtime;

namespace Application.BusinessServices.Devices.Realtime;

public sealed class DeviceRealtimeNotifier : IDeviceRealtimeNotifier
{
    private readonly IRealtimePublisher _publisher;

    public DeviceRealtimeNotifier(IRealtimePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishDelta(
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

        await _publisher.PublishToDevice(deviceId, realtimeDelta, cancellationToken);

        if (roomId.HasValue)
            await _publisher.PublishToRoom(roomId.Value, realtimeDelta, cancellationToken);

        if (previousRoomId.HasValue && previousRoomId != roomId)
            await _publisher.PublishToRoom(previousRoomId.Value, realtimeDelta, cancellationToken);

        if (homeId.HasValue)
            await _publisher.PublishToHome(homeId.Value, realtimeDelta, cancellationToken);
    }

    public async Task PublishDeleted(
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

        await _publisher.PublishToDevice(deviceId, realtimeDelta, cancellationToken);

        if (roomId.HasValue)
            await _publisher.PublishToRoom(roomId.Value, realtimeDelta, cancellationToken);

        if (homeId.HasValue)
            await _publisher.PublishToHome(homeId.Value, realtimeDelta, cancellationToken);
    }
}
