namespace Core.Domain.Homes;

public interface IHomeRepository
{
    Task<Home?> GetById(Guid id, CancellationToken ct = default);
    Task Add(Home home, CancellationToken ct = default);
    void Remove(Home home);
}
