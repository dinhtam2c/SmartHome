using Application.BusinessServices.Capabilities.Validation;
using Application.Common.Errors;
using Domain.Models.Devices;
using Microsoft.Extensions.Logging;

namespace Application.BusinessServices.Devices.State;

public sealed class CapabilityStateUpdater : ICapabilityStateUpdater
{
    private readonly ILogger<CapabilityStateUpdater> _logger;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ICapabilityStateValidator _capabilityStateValidator;

    public CapabilityStateUpdater(
        ILogger<CapabilityStateUpdater> logger,
        IDeviceRepository deviceRepository,
        ICapabilityStateValidator capabilityStateValidator)
    {
        _logger = logger;
        _deviceRepository = deviceRepository;
        _capabilityStateValidator = capabilityStateValidator;
    }

    public async Task<IReadOnlyList<CapabilityStateUpdate>> Apply(
        Guid deviceId,
        IReadOnlyCollection<CapabilityStateUpdate> stateChanges,
        CancellationToken cancellationToken)
    {
        if (stateChanges.Count == 0)
            return [];

        var device = await _deviceRepository.GetById(deviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(deviceId);

        if (!device.IsOnline)
        {
            _logger.LogWarning(
                "Device {DeviceId} is offline, ignoring {StateChangeCount} capability state change(s)",
                deviceId,
                stateChanges.Count);
            return [];
        }

        var applied = new List<CapabilityStateUpdate>(stateChanges.Count);
        foreach (var stateChange in stateChanges)
        {
            if (string.IsNullOrWhiteSpace(stateChange.EndpointId))
            {
                _logger.LogWarning(
                    "Capability state for {CapabilityId} is missing endpointId on device {DeviceId}",
                    stateChange.CapabilityId,
                    deviceId);
                continue;
            }

            var capability = device.FindCapability(
                stateChange.CapabilityId,
                stateChange.EndpointId);
            if (capability is null)
            {
                _logger.LogWarning(
                    "Capability {CapabilityId} endpoint {EndpointId} not found on device {DeviceId}",
                    stateChange.CapabilityId,
                    stateChange.EndpointId,
                    deviceId);
                continue;
            }

            Dictionary<string, object?> normalizedState;
            try
            {
                normalizedState = _capabilityStateValidator.NormalizeAndValidate(
                    capability,
                    stateChange.State);
            }
            catch (InvalidCapabilityStatePayloadException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid state payload for capability {CapabilityId} endpoint {EndpointId} on device {DeviceId}",
                    stateChange.CapabilityId,
                    stateChange.EndpointId,
                    deviceId);
                continue;
            }

            device.UpdateCapabilityState(capability, normalizedState);
            applied.Add(new CapabilityStateUpdate(
                capability.CapabilityId,
                stateChange.EndpointId.Trim(),
                normalizedState));
        }

        return applied;
    }
}
