namespace Application.Common.Realtime;

public interface IRealtimeDetailsNotifier
{
    Task PublishDeviceDetailsChanged(Guid deviceId, CancellationToken cancellationToken = default);

    Task PublishDeviceDeleted(
        Guid deviceId,
        Guid? homeId,
        Guid? roomId,
        CancellationToken cancellationToken = default);

    Task PublishRoomDetailsChanged(
        Guid homeId,
        Guid roomId,
        CancellationToken cancellationToken = default);

    Task PublishRoomDeleted(
        Guid homeId,
        Guid roomId,
        CancellationToken cancellationToken = default);
}