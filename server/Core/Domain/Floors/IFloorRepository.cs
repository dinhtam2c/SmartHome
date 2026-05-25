namespace Core.Domain.Floors;

public interface IFloorRepository
{
    Task<Floor?> GetById(Guid id, CancellationToken ct = default);
    Task<List<Floor>> ListByHomeId(Guid homeId, CancellationToken ct = default);
    Task<int> GetNextSortOrder(Guid homeId, CancellationToken ct = default);
    Task<bool> HasPlacedDevice(Guid homeId, Guid deviceId, CancellationToken ct = default);
    Task Add(Floor floor, CancellationToken ct = default);
    void Remove(Floor floor);
}
