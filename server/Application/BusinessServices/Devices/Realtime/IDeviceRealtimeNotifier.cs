namespace Application.BusinessServices.Devices.Realtime;

public interface IDeviceRealtimeNotifier
{
    Task PublishDelta(
        Guid deviceId,
        Guid? homeId,
        Guid? roomId,
        string change,
        object? delta,
        Guid? previousRoomId = null,
        CancellationToken cancellationToken = default);

    Task PublishDeleted(
        Guid deviceId,
        Guid? homeId,
        Guid? roomId,
        object? delta = null,
        CancellationToken cancellationToken = default);
}
