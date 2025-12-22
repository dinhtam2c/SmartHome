using Core.Entities;

namespace Application.Interfaces.Repositories;

public interface IHomeRepository
{
    Task Add(Home home);

    Task<IEnumerable<Home>> GetAll();

    Task<Home?> GetById(Guid id);

    Task<Home?> GetByIdWithLocations(Guid id);

    Task Delete(Home home);
}
