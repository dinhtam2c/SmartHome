using Application.DTOs.DashboardDto;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IDashboardService
{
    Task<IEnumerable<HomeDashboardElementDto>> GetHomeListDashboard();

    Task<HomeDashboardDto> GetHomeDashboard(Guid homeId);

    Task<LocationDashboardDto> GetLocationDashboard(Guid locationId);

    Task<DeviceDashboardDto> GetDeviceDashboard(Guid deviceId);
}

public class DashboardService : IDashboardService
{
    private readonly ILogger<DashboardService> _logger;
    private readonly IHomeRepository _homeRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ISensorDataRepository _sensorDataRepository;

    public DashboardService(ILogger<DashboardService> logger, IHomeRepository homeRepository,
        ILocationRepository locationRepository, IDeviceRepository deviceRepository,
        ISensorDataRepository sensorDataRepository)
    {
        _logger = logger;
        _homeRepository = homeRepository;
        _locationRepository = locationRepository;
        _deviceRepository = deviceRepository;
        _sensorDataRepository = sensorDataRepository;
    }

    public async Task<IEnumerable<HomeDashboardElementDto>> GetHomeListDashboard()
    {
        var homes = await _homeRepository.GetAll();
        var list = homes.Select(home => new HomeDashboardElementDto(home));
        return list;
    }

    public async Task<HomeDashboardDto> GetHomeDashboard(Guid homeId)
    {
        var home = await _homeRepository.GetByIdWithLocations(homeId);
        if (home == null)
            throw new HomeNotFoundException(homeId);

        var locationDeviceCounts = await _deviceRepository.CountByLocationForHome(homeId);

        var homeDeviceCount = 0;
        var homeOnlineDeviceCount = 0;
        foreach (var count in locationDeviceCounts)
        {
            homeDeviceCount += count.Value.Total;
            homeOnlineDeviceCount += count.Value.Online;
        }

        var summary = new HomeDashboardSummary(homeDeviceCount, homeOnlineDeviceCount);

        var locations = home.Locations.Select(l =>
        {
            var hasCount = locationDeviceCounts.TryGetValue(l.Id, out var count);
            return new LocationElement(
                l.Id,
                l.Name,
                l.Description,
                hasCount ? count!.Total : 0,
                hasCount ? count!.Online : 0
            );
        });

        return new(home, summary, locations);
    }

    public async Task<LocationDashboardDto> GetLocationDashboard(Guid locationId)
    {
        var location = await _locationRepository.GetByIdWithDevicesWithSensorsAndActuators(locationId);
        if (location == null)
            throw new LocationNotFoundException(locationId);

        var deviceCount = location.Devices.Count();
        var onlineDeviceCount = location.Devices.Count(d => d.IsOnline);

        var summary = new LocationDashboardSummary(deviceCount, onlineDeviceCount);

        var sensorIds = location.Devices.SelectMany(d => d.Sensors).Select(s => s.Id);
        var sensorData = await _sensorDataRepository.GetLatestBySensorIds(sensorIds);

        var devices = location.Devices.Select(device =>
        {
            var deviceSensorData = device.Sensors
                .Where(s => sensorData.ContainsKey(s.Id))
                .Select(s =>
                {
                    var data = sensorData[s.Id];
                    return new SensorDataDto(s.Id, s.Name, s.Type, s.Unit, data.Value, data.Timestamp);
                });

            var actuatorStates = device.Actuators
                .Select(a => new ActuatorStateDto(a.Id, a.Name, device.IsOnline ? a.States : null));

            return new DeviceElement(device.Id, device.Name, device.IsOnline, deviceSensorData, actuatorStates);
        });

        return new(location, summary, devices);
    }

    public async Task<DeviceDashboardDto> GetDeviceDashboard(Guid deviceId)
    {
        var device = await _deviceRepository.GetByIdWithSensorsAndActuators(deviceId);
        if (device == null)
            throw new DeviceNotFoundException(deviceId);

        var sensorIds = device.Sensors.Select(s => s.Id);
        var sensorData = await _sensorDataRepository.GetLatestBySensorIds(sensorIds);

        var latestSensorData = device.Sensors
            .Where(s => sensorData.ContainsKey(s.Id))
            .Select(s =>
            {
                var data = sensorData[s.Id];
                return new SensorDataDto(s.Id, s.Name, s.Type, s.Unit, data.Value, data.Timestamp);
            });

        return new(device, latestSensorData);
    }
}
