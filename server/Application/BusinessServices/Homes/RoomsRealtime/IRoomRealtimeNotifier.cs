namespace Application.BusinessServices.Homes.RoomsRealtime;

public interface IRoomRealtimeNotifier
{
    Task PublishDelta(
        Guid homeId,
        Guid roomId,
        string change,
        object? delta,
        CancellationToken cancellationToken = default);

    Task PublishDeleted(
        Guid homeId,
        Guid roomId,
        CancellationToken cancellationToken = default);
}
