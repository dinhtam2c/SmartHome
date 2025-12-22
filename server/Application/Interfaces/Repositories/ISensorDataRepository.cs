using Core.Entities;

namespace Application.Interfaces.Repositories;

public interface ISensorDataRepository
{
    Task AddRange(IEnumerable<SensorData> sensorData);
}
