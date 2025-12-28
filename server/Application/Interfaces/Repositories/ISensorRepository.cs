using Core.Entities;

namespace Application.Interfaces.Repositories;

public interface ISensorRepository
{
    Task<Sensor?> GetById(Guid id);
}
