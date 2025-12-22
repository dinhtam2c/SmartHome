using Core.Entities;

namespace Application.Interfaces.Repositories;

public interface ILocationRepository
{
    Task Add(Location location);

    Task<IEnumerable<Location>> GetAll();

    Task<Location?> GetById(Guid id);

    Task<Location?> GetByIdWithDevicesWithSensorsAndActuators(Guid id);

    Task Delete(Location location);
}
