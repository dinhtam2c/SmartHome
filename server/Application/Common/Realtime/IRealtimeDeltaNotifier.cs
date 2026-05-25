namespace Application.Common.Realtime;

public interface IRealtimeDeltaNotifier
{
    Task PublishDeviceDelta(
        Guid deviceId,
        Guid? homeId,
        Guid? roomId,
        string change,
        object? delta,
        Guid? previousRoomId = null,
        CancellationToken cancellationToken = default);

    Task PublishDeviceDeleted(
        Guid deviceId,
        Guid? homeId,
        Guid? roomId,
        object? delta = null,
        CancellationToken cancellationToken = default);

    Task PublishRoomDelta(
        Guid homeId,
        Guid roomId,
        string change,
        object? delta,
        CancellationToken cancellationToken = default);

    Task PublishRoomDeleted(
        Guid homeId,
        Guid roomId,
        CancellationToken cancellationToken = default);
}
