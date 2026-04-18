using System.Text.Json;
using Application.Common.Data;
using Application.Common.Json;
using Application.Exceptions;
using Application.Services;
using Core.Domain.Devices;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Devices.UpdateDeviceCapabilitiesState;

public sealed class UpdateDeviceCapabilitiesStateCommandHandler
    : IRequestHandler<UpdateDeviceCapabilitiesStateCommand>
{
    private readonly ILogger<UpdateDeviceCapabilitiesStateCommandHandler> _logger;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ICapabilityStateValidator _capabilityStateValidator;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDeviceCapabilitiesStateCommandHandler(
        ILogger<UpdateDeviceCapabilitiesStateCommandHandler> logger,
        IDeviceRepository deviceRepository,
        ICapabilityStateValidator capabilityStateValidator,
        IUnitOfWork unitOfWork
    )
    {
        _logger = logger;
        _deviceRepository = deviceRepository;
        _capabilityStateValidator = capabilityStateValidator;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateDeviceCapabilitiesStateCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetById(request.DeviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        if (!device.IsOnline)
        {
            _logger.LogWarning("Device {DeviceId} is offline, ignoring capability state update", request.DeviceId);
            return;
        }

        _logger.LogInformation("Received capabilities state from device {DeviceId}", request.DeviceId);

        foreach (var capabilityState in request.States)
        {
            if (string.IsNullOrWhiteSpace(capabilityState.EndpointId))
            {
                _logger.LogWarning(
                    "Capability state for {CapabilityId} is missing endpointId on device {DeviceId}",
                    capabilityState.CapabilityId,
                    request.DeviceId);
                continue;
            }

            var normalizedState = NormalizeStateValues(capabilityState.State);

            var capability = device.FindSingleCapability(
                capabilityState.CapabilityId,
                capabilityState.EndpointId,
                out _
            );

            if (capability is null)
            {
                _logger.LogWarning(
                    "Capability {CapabilityId} endpoint {EndpointId} not found on device {DeviceId}",
                    capabilityState.CapabilityId,
                    capabilityState.EndpointId,
                    request.DeviceId
                );
                continue;
            }

            try
            {
                _capabilityStateValidator.Validate(capability!, normalizedState);
            }
            catch (InvalidCapabilityStatePayloadException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid state payload for capability {CapabilityId} endpoint {EndpointId} on device {DeviceId}",
                    capabilityState.CapabilityId,
                    capabilityState.EndpointId,
                    request.DeviceId);
                continue;
            }

            device.UpdateCapabilityState(capability!, normalizedState);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, object?> NormalizeStateValues(Dictionary<string, object?> state)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in state)
        {
            result[item.Key] = item.Value is JsonElement element
                ? JsonHelper.JsonElementToObject(element)
                : item.Value;
        }

        return result;
    }
}
