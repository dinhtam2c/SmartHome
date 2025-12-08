using Core.Entities;

namespace Application.Interfaces.Repositories;

public interface IGatewayRepository
{
    Task Add(Gateway gateway);

    Task<IEnumerable<Gateway>> GetAllWithHome();

    Task<Gateway?> GetById(Guid id);

    Task<Gateway?> GetByMac(string mac);
}
