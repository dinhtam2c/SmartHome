using Application.DTOs.Messages.Gateways;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface ISensorDataService
{
    Task StoreSensorData(Guid gatewayId, GatewayData gatewayData);
}

public class SensorDataService : ISensorDataService
{
    private readonly ILogger<SensorDataService> _logger;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ISensorDataRepository _sensorDataRepository;
    private readonly IDeviceRepository _deviceRepository;

    private readonly IGatewayService _gatewayService;
    private readonly IDeviceService _deviceService;

    public SensorDataService(ILogger<SensorDataService> logger, IUnitOfWork unitOfWork,
        ISensorDataRepository sensorDataRepository, IDeviceRepository deviceRepository,
        IGatewayService gatewayService, IDeviceService deviceService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _sensorDataRepository = sensorDataRepository;
        _deviceRepository = deviceRepository;
        _gatewayService = gatewayService;
        _deviceService = deviceService;
    }

    public async Task StoreSensorData(Guid gatewayId, GatewayData gatewayData)
    {
        await _gatewayService.EnsureGatewayExistOrReprovision(gatewayId);

        _logger.LogInformation("Storing sensor data for Gateway {GatewayId} with {DataCount} data points",
            gatewayId, gatewayData.Data.Count());

        var deviceIds = gatewayData.Data.Select(d => d.DeviceId);
        var devices = await _deviceRepository.GetByIdsWithLocationAndSensors(deviceIds);
        var deviceMap = devices.ToDictionary(d => d.Id);

        var sensorDataToInsert = new List<SensorData>();

        foreach (var deviceData in gatewayData.Data)
        {
            if (!deviceMap.TryGetValue(deviceData.DeviceId, out var device))
            {
                _ = _deviceService.SendReprovision(gatewayId, deviceData.DeviceId);
                continue;
            }

            long timestamp = deviceData.Timestamp;

            var sensorMap = device.Sensors.ToDictionary(s => s.Id);

            var sensorData = deviceData.Data
                .Select(e =>
                {
                    if (!sensorMap.TryGetValue(e.SensorId, out var sensor))
                        return null;

                    return e.ToSensorData(device.LocationId, timestamp);
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
}
