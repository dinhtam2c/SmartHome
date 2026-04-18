namespace Application.Common.Realtime;

public interface IRealtimePublisher
{
    Task PublishToHome(
        Guid homeId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);

    Task PublishToRoom(
        Guid roomId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);

    Task PublishToDevice(
        Guid deviceId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);
}
