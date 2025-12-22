using Application.DTOs.DeviceDto;
using Core.Entities;

namespace Application.Interfaces.Repositories;

public interface IDeviceRepository
{
    Task Add(Device device);

    Task<Device?> GetById(Guid id);

    Task<IEnumerable<Device>> GetAllWithGatewayAndHomeAndLocation();

    Task<IEnumerable<Device>> GetByIdsWithLocationAndSensors(IEnumerable<Guid> id);

    Task<Device?> GetByIdWithGatewayAndLocationAndCapabilities(Guid id);

    Task<Device?> GetByIdWithActuators(Guid id);

    Task<Device?> GetByIdWithSensorsAndActuators(Guid id);

    Task<Device?> GetByIdentifierWithCapabilities(string deviceIdentifier);

    Task<Dictionary<Guid, LocationDeviceCount>> CountByLocationForHome(Guid homeId);
}
