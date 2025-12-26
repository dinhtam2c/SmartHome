using System.Text.Json;
using Application.Common.Message;
using Application.DTOs.DeviceDto;
using Application.DTOs.ProvisionDto;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IDeviceService
{
    Task<IEnumerable<DeviceListElement>> GetAllDevices();

    Task<DeviceDetails> GetDeviceDetails(Guid deviceId);

    Task<DeviceAddResponse> AddDevice(DeviceAddRequest request);

    Task EnsureDeviceExistOrReprovision(Guid gatewayId, Guid deviceId);

    Task SendReprovision(Guid gatewayId, Guid deviceId);

    Task DeviceProvision(Guid gatewayId, DeviceProvisionRequest request);

    Task HandleDeviceAvailability(Guid gatewayId, Guid deviceId, DeviceAvailability availability);

    Task HandleDeviceActuatorsStates(Guid gatewayId, Guid deviceId, IEnumerable<DeviceActuatorStates> states);

    Task SendDeviceCommand(Guid deviceId, DeviceCommandRequest deviceCommandRequest);

    Task AssignLocationToDevice(Guid deviceId, DeviceLocationAssignRequest request);

    Task AssignGatewayToDevice(Guid deviceId, DeviceGatewayAssignRequest request);

    Task UpdateDevice(Guid deviceId, DeviceUpdateRequest request);

    Task DeleteDevice(Guid deviceId);
}

public class DeviceService : IDeviceService
{
    private readonly ILogger<DeviceService> _logger;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocationRepository _locationRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IGatewayRepository _gatewayRepository;

    private readonly IGatewayService _gatewayService;

    private readonly IMessagePublisher _messagePublisher;

    public DeviceService(ILogger<DeviceService> logger, IUnitOfWork unitOfWork,
        ILocationRepository locationRepository, IDeviceRepository deviceRepository,
        IGatewayRepository gatewayRepository, IGatewayService gatewayService, IMessagePublisher messagePublisher)
    {
        _logger = logger;
        _locationRepository = locationRepository;
        _deviceRepository = deviceRepository;
        _gatewayRepository = gatewayRepository;
        _gatewayService = gatewayService;
        _unitOfWork = unitOfWork;
        _messagePublisher = messagePublisher;
    }

    public async Task<IEnumerable<DeviceListElement>> GetAllDevices()
    {
        var devices = await _deviceRepository.GetAllWithGatewayAndHomeAndLocation();
        return devices.Select(DeviceListElement.FromDevice);
    }

    public async Task<DeviceDetails> GetDeviceDetails(Guid deviceId)
    {
        var device = await _deviceRepository.GetByIdWithGatewayAndLocationAndCapabilities(deviceId);
        if (device is null)
            throw new DeviceNotFoundException(deviceId);

        return DeviceDetails.FromDevice(device);
    }

    public async Task<DeviceAddResponse> AddDevice(DeviceAddRequest request)
    {
        var device = request.ToDevice();
        if (device.GatewayId.HasValue)
        {
            await _gatewayService.EnsureGatewayExistOrReprovision(device.GatewayId.Value);
        }

        await _deviceRepository.Add(device);
        await _unitOfWork.Commit();

        // TODO: add to whitelist

        return DeviceAddResponse.FromDevice(device);
    }

    public async Task SendReprovision(Guid gatewayId, Guid deviceId)
    {
        try
        {
            _logger.LogWarning("Sending reprovision request for device {DeviceId}", deviceId);

            var topic = MessageTopics.DeviceProvisionResponse(gatewayId.ToString(), deviceId.ToString());
            var payload = new DeviceProvisionResponse(deviceId.ToString(), null, null, null);

            await _messagePublisher.PublishMessage(topic, payload, new(0, false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reprovision message for device {DeviceId}", deviceId);
        }
    }

    public async Task EnsureDeviceExistOrReprovision(Guid gatewayId, Guid deviceId)
    {
        var device = await _deviceRepository.GetById(deviceId);
        if (device is not null)
            return;

        await SendReprovision(gatewayId, deviceId);
        throw new DeviceNotFoundException(deviceId);
    }

    public async Task DeviceProvision(Guid gatewayId, DeviceProvisionRequest request)
    {
        await _gatewayService.EnsureGatewayExistOrReprovision(gatewayId);
        var device = await _deviceRepository.GetByIdentifierWithCapabilities(request.Identifier);

        if (device is null)
        {
            _logger.LogError("Device with identifier {Identifier} not found", request.Identifier);
            throw new DeviceNotFoundException(request.Identifier);
        }

        if (device.GatewayId != gatewayId)
        {
            // TODO: custom exception
            throw new Exception("Device provisioning: GatewayId mismatch");
        }

        _logger.LogInformation("Provisioning device {DeviceId} for gateway {GatewayId}", device.Id, gatewayId);

        /* Metadata */
        if (string.IsNullOrEmpty(device.Name) && !string.IsNullOrEmpty(request.Name))
            device.Name = request.Name;
        device.Manufacturer = request.Manufacturer;
        device.Model = request.Model;
        device.FirmwareVersion = request.FirmwareVersion;
        device.LastSeenAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /* Capabilities */
        device.Sensors = request.Sensors?.Select(s => s.ToSensor(device.Id)).ToList() ?? [];
        device.Actuators = request.Actuators?.Select(a => a.ToActuator(device.Id)).ToList() ?? [];

        device.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _unitOfWork.Commit();

        var sensorIds = device.Sensors.Count != 0 ? device.Sensors.Select(s => s.Id) : null;
        var actuatorIds = device.Actuators.Count != 0 ? device.Actuators.Select(a => a.Id) : null;

        var topic = MessageTopics.DeviceProvisionResponse(gatewayId.ToString(), request.Identifier);
        var response = new DeviceProvisionResponse(device.Identifier, device.Id, sensorIds, actuatorIds);

        await _messagePublisher.PublishMessage(topic, response, new(1, false));
    }

    public async Task HandleDeviceAvailability(Guid gatewayId, Guid deviceId, DeviceAvailability availability)
    {
        var gateway = await _gatewayRepository.GetById(gatewayId);
        if (gateway is null)
        {
            await _gatewayService.SendReprovision(gatewayId);
            throw new GatewayNotFoundException(gatewayId);
        }

        var device = await _deviceRepository.GetById(deviceId);
        if (device is null)
        {
            await SendReprovision(gatewayId, deviceId);
            throw new DeviceNotFoundException(deviceId);
        }

        if (!gateway.IsOnline)
            return;


        if (availability.State == "Online")
        {
            device.IsOnline = true;
            device.LastSeenAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        else if (availability.State == "Offline")
        {
            device.IsOnline = false;
            device.UpTime = 0;
        }
        else
        {
            // TODO: custom exception
            throw new Exception($"Device {deviceId} sent unknown state: {availability.State}");
        }

        await _unitOfWork.Commit();
        _logger.LogInformation("Device {DeviceId} {Status}", deviceId, availability.State);
    }

    public async Task HandleDeviceActuatorsStates(Guid gatewayId, Guid deviceId,
        IEnumerable<DeviceActuatorStates> states)
    {
        await _gatewayService.EnsureGatewayExistOrReprovision(gatewayId);

        var device = await _deviceRepository.GetByIdWithActuators(deviceId);
        if (device is null)
        {
            await SendReprovision(gatewayId, deviceId);
            throw new DeviceNotFoundException(deviceId);
        }

        if (!device.IsOnline)
        {
            _logger.LogWarning("Device {DeviceId} is offline", deviceId);
            return;
        }

        _logger.LogInformation("Received actuators's states from device {DeviceId}", deviceId);

        foreach (var actuatorStates in states)
        {
            var actuator = device.Actuators.FirstOrDefault(a => a.Id == actuatorStates.ActuatorId);
            if (actuator is null)
            {
                _logger.LogWarning("Actuator {ActuatorId} not found", actuatorStates.ActuatorId);
                await SendReprovision(gatewayId, deviceId);
                break;
            }

            if (actuator.States is null)
            {
                _logger.LogWarning("Actuator {ActuatorId} does not have states", actuator.Id);
                continue;
            }

            foreach (var state in actuatorStates.States)
            {
                if (!actuator.States.ContainsKey(state.Key))
                {
                    _logger.LogWarning("Actuator {ActuatorId} does not have state {state}",
                        actuator.Id, nameof(state.Key));
                    continue;
                }

                actuator.States[state.Key] = ((JsonElement)state.Value).GetString();
            }
        }

        await _unitOfWork.Commit(); // TODO: update only if no reprovision
    }

    public async Task SendDeviceCommand(Guid deviceId, DeviceCommandRequest deviceCommandRequest)
    {
        // TODO: no need to get location and sensors
        var device = await _deviceRepository.GetByIdWithGatewayAndLocationAndCapabilities(deviceId) ??
            throw new DeviceNotFoundException(deviceId);

        // TODO: custom exception
        var gateway = device.Gateway ??
            throw new Exception("Device does not belong to any gateway");

        var actuator = device.Actuators?.FirstOrDefault(a => a.Id == deviceCommandRequest.ActuatorId) ??
            throw new ActuatorNotFoundException(deviceCommandRequest.ActuatorId);

        // TODO: custom exception
        if (!actuator.SupportedCommands?.Contains(deviceCommandRequest.Command) ?? false)
        {
            throw new Exception("Command not supported");
        }

        _logger.LogInformation("Sending command {Command} to device {DeviceId}",
            deviceCommandRequest.ToString(), deviceId);

        var topic = MessageTopics.DeviceCommand(gateway.Id.ToString(), device.Id.ToString());
        var payload = new DeviceCommand(deviceId, actuator.Id,
            deviceCommandRequest.Command, deviceCommandRequest.Parameters);

        await _messagePublisher.PublishMessage(topic, payload, new(2, false));
    }

    public async Task AssignLocationToDevice(Guid deviceId, DeviceLocationAssignRequest request)
    {
        var locationId = request.LocationId;

        var device = await _deviceRepository.GetById(deviceId) ?? throw new DeviceNotFoundException(deviceId);
        var _ = await _locationRepository.GetById(locationId) ?? throw new LocationNotFoundException(locationId);

        device.LocationId = locationId;
        device.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _unitOfWork.Commit();
    }

    public async Task AssignGatewayToDevice(Guid deviceId, DeviceGatewayAssignRequest request)
    {
        var gatewayId = request.GatewayId;

        var device = await _deviceRepository.GetById(deviceId) ?? throw new DeviceNotFoundException(deviceId);
        var _ = await _gatewayRepository.GetById(gatewayId) ?? throw new GatewayNotFoundException(gatewayId);

        device.GatewayId = gatewayId;
        device.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _unitOfWork.Commit();

        await _gatewayService.AddDeviceToWhiteList(gatewayId, device.Identifier);
    }

    public async Task UpdateDevice(Guid deviceId, DeviceUpdateRequest request)
    {
        var device = await _deviceRepository.GetById(deviceId) ?? throw new DeviceNotFoundException(deviceId);
        device.Name = request.Name;
        device.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _unitOfWork.Commit();
    }

    public async Task DeleteDevice(Guid deviceId)
    {
        var device = await _deviceRepository.GetById(deviceId) ?? throw new DeviceNotFoundException(deviceId);
        await _deviceRepository.Delete(device);
        await _unitOfWork.Commit();
    }
}
