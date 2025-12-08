using Application.DTOs.SensorDataDto;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface ISensorDataService
{
    Task StoreSensorData(GatewayData gatewayData);

    Task<IEnumerable<SensorDataResponse>> GetAllSensorData();
}

public class SensorDataService : ISensorDataService
{
    private readonly ILogger<SensorDataService> _logger;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ISensorDataRepository _sensorDataRepository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IDeviceRepository _deviceRepository;

    private readonly IGatewayService _gatewayService;
    private readonly IDeviceService _deviceService;

    public SensorDataService(ILogger<SensorDataService> logger, IUnitOfWork unitOfWork,
        ISensorDataRepository sensorDataRepository, ISensorRepository sensorRepository,
        IDeviceRepository deviceRepository, IGatewayService gatewayService, IDeviceService deviceService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _sensorDataRepository = sensorDataRepository;
        _sensorRepository = sensorRepository;
        _deviceRepository = deviceRepository;
        _gatewayService = gatewayService;
        _deviceService = deviceService;
    }

    public async Task StoreSensorData(GatewayData gatewayData)
    {
        await _gatewayService.EnsureGatewayExistOrReprovision(gatewayData.GatewayId);

        _logger.LogInformation("Storing sensor data for Gateway {GatewayId} with {DataCount} data points",
            gatewayData.GatewayId, gatewayData.Data.Count());

        var deviceIds = gatewayData.Data.Select(d => d.DeviceId);
        var devices = await _deviceRepository.GetByIdsWithLocationAndSensors(deviceIds);
        var deviceMap = devices.ToDictionary(d => d.Id);

        var sensorDataToInsert = new List<SensorData>();

        foreach (var deviceData in gatewayData.Data)
        {
            if (!deviceMap.TryGetValue(deviceData.DeviceId, out var device))
            {
                _ = _deviceService.SendReprovision(gatewayData.GatewayId, deviceData.DeviceId);
                continue;
            }

            string? location = device.Location?.Name ?? "Unknown";
            long timestamp = deviceData.Timestamp;

            var sensorMap = device.Sensors.ToDictionary(s => s.Id);

            var sensorData = deviceData.Data
                .Select(e =>
                {
                    if (!sensorMap.TryGetValue(e.SensorId, out var sensor))
                        return null;

                    return e.ToSensorData(location, sensor.Type, sensor.Unit, timestamp);
                })
                .Where(sd => sd is not null)
                .Cast<SensorData>();

            sensorDataToInsert.AddRange(sensorData);
        }

        if (sensorDataToInsert.Count > 0)
        {
            await _sensorDataRepository.AddRange(sensorDataToInsert);
            await _unitOfWork.Commit();
        }
    }

    public async Task<IEnumerable<SensorDataResponse>> GetAllSensorData()
    {
        return (await _sensorDataRepository.GetAllWithSensor()).Select(SensorDataResponse.FromSensorData);
    }
}
