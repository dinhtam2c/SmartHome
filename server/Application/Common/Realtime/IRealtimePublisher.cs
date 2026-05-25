namespace Application.Common.Realtime;

public interface IRealtimePublisher
{
    Task PublishToHome(
        Guid homeId,
        RealtimeDelta delta,
        CancellationToken cancellationToken = default);

    Task PublishToRoom(
        Guid roomId,
        RealtimeDelta delta,
        CancellationToken cancellationToken = default);

    Task PublishToDevice(
        Guid deviceId,
        RealtimeDelta delta,
        CancellationToken cancellationToken = default);
}
