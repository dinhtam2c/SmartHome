using Application.Common.Message;
using Application.DTOs.GatewayDto;
using Application.DTOs.ProvisionDto;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IGatewayService
{
    Task<IEnumerable<GatewayListElement>> GetAllGateways();

    Task SendReprovision(Guid gatewayId);

    Task EnsureGatewayExistOrReprovision(Guid gatewayId);

    Task GatewayProvision(GatewayProvisionRequest request);

    Task HandleGatewayAvailability(Guid gatewayId, GatewayAvailability availability);

    Task AssignDeviceToGateway(Guid gatewayId, Guid deviceId);

    Task AddDeviceToWhiteList(Guid gatewayId, string identifier);
}

public class GatewayService : IGatewayService
{
    private readonly ILogger<GatewayService> _logger;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IDeviceRepository _deviceRepository;

    private readonly IMessagePublisher _messagePublisher;

    public GatewayService(ILogger<GatewayService> logger, IUnitOfWork unitOfWork,
        IGatewayRepository gatewayRepository, IDeviceRepository deviceRepository,
        IMessagePublisher messagePublisher)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _gatewayRepository = gatewayRepository;
        _deviceRepository = deviceRepository;
        _messagePublisher = messagePublisher;
    }

    public async Task<IEnumerable<GatewayListElement>> GetAllGateways()
    {
        var gateways = await _gatewayRepository.GetAllWithHome();
        return gateways.Select(GatewayListElement.FromGateway);
    }

    public async Task SendReprovision(Guid gatewayId)
    {
        try
        {
            _logger.LogWarning("Sending reprovision request for gateway {GatewayId}", gatewayId);

            var topic = MessageTopics.GatewayProvisionResponse(gatewayId.ToString());
            var payload = new GatewayProvisionResponse(null);

            await _messagePublisher.PublishMessage(topic, payload, new(0, false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reprovision message for gateway {GatewayId}", gatewayId);
        }
    }

    public async Task EnsureGatewayExistOrReprovision(Guid gatewayId)
    {
        var gateway = await _gatewayRepository.GetById(gatewayId);
        if (gateway is not null)
            return;

        await SendReprovision(gatewayId);
        throw new GatewayNotFoundException(gatewayId);
    }

    public async Task GatewayProvision(GatewayProvisionRequest request)
    {
        // TODO: Validate Key

        Gateway? gateway = await _gatewayRepository.GetByMac(request.Mac);
        if (gateway is null)
        {
            _logger.LogInformation("Provisioning new gateway with MAC {Mac}", request.Mac);
            gateway = request.ToGateway();
            await _gatewayRepository.Add(gateway);
        }
        else
        {
            _logger.LogInformation("Updating existing gateway with MAC {Mac}", request.Mac);
            gateway.Manufacturer = request.Manufacturer;
            gateway.Model = request.Model;
            gateway.FirmwareVersion = request.FirmwareVersion;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            gateway.LastSeenAt = now;
            gateway.UpdatedAt = now;
        }

        await _unitOfWork.Commit();

        var topic = MessageTopics.GatewayProvisionResponse(request.Mac);
        var response = new GatewayProvisionResponse(gateway.Id);

        await _messagePublisher.PublishMessage(topic, response, new(1, false));
    }

    public async Task HandleGatewayAvailability(Guid gatewayId, GatewayAvailability availability)
    {
        var gateway = await _gatewayRepository.GetByIdWithDevices(gatewayId);   // TODO: optimize
        if (gateway is null)
        {
            await SendReprovision(gatewayId);
            throw new GatewayNotFoundException(gatewayId);
        }

        if (availability.State == "Online")
        {
            gateway.IsOnline = true;
            gateway.LastSeenAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        else if (availability.State == "Offline")
        {
            gateway.IsOnline = false;
            gateway.UpTime = 0;
            gateway.DeviceCount = 0;
            foreach (var device in gateway.Devices)
            {
                device.IsOnline = false;
            }
        }
        else
        {
            // TODO: custom exception
            throw new Exception($"Unknown state {availability.State}");
        }

        await _unitOfWork.Commit();
        _logger.LogInformation("Gateway {GatewayId} {Status}", gatewayId, availability.State);
    }

    public async Task AssignDeviceToGateway(Guid gatewayId, Guid deviceId)
    {
        var gateway = await _gatewayRepository.GetById(gatewayId) ?? throw new GatewayNotFoundException(gatewayId);
        var device = await _deviceRepository.GetById(deviceId) ?? throw new DeviceNotFoundException(deviceId);

        device.GatewayId = gatewayId;
        device.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await AddDeviceToWhiteList(gatewayId, device.Identifier);

        await _unitOfWork.Commit();
    }

    public async Task AddDeviceToWhiteList(Guid gatewayId, string identifier)
    {
        // TODO: send message to gateway
    }
}
