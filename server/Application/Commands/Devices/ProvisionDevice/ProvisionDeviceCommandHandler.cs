using Application.Common.Data;
using Application.Services;
using Core.Domain.Devices;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Devices.ProvisionDevice;

public sealed class ProvisionDeviceCommandHandler : IRequestHandler<ProvisionDeviceCommand>
{
    private readonly ILogger<ProvisionDeviceCommandHandler> _logger;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityRegistryValidator _capabilityRegistryValidator;

    public ProvisionDeviceCommandHandler(
        ILogger<ProvisionDeviceCommandHandler> logger,
        IDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityRegistryValidator capabilityRegistryValidator)
    {
        _deviceRepository = deviceRepository;
        _logger = logger;
        _capabilityRegistry = capabilityRegistry;
        _capabilityRegistryValidator = capabilityRegistryValidator;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ProvisionDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByMacAddress(request.MacAddress);

        if (device is null)
        {
            device = Device.Create(request.MacAddress);
            await _deviceRepository.Add(device);
        }

        _logger.LogInformation("Provisioning device {DeviceId}", device.Id);

        var validatedEndpoints = _capabilityRegistryValidator.ValidateAndNormalize(request.Endpoints);

        var endpoints = validatedEndpoints
            .Select(endpointModel =>
            {
                var endpoint = new DeviceEndpoint(device.Id, endpointModel.EndpointId, endpointModel.Name);
                var endpointCapabilities = endpointModel.Capabilities
                    .Select(capability => capability.ToDeviceCapability(endpoint.Id))
                    .ToList();

                endpoint.ReplaceCapabilities(endpointCapabilities);
                return endpoint;
            })
            .ToList();

        var capabilityDefinitions = validatedEndpoints
            .SelectMany(endpoint => endpoint.Capabilities)
            .Select(c => _capabilityRegistry.GetRequiredDefinition(c.CapabilityId, c.CapabilityVersion))
            .DistinctBy(definition => $"{definition.Id.ToLowerInvariant()}@{definition.Version}")
            .ToList();

        device.Reprovision(
            request.Name,
            request.FirmwareVersion,
            request.Protocol,
            endpoints,
            capabilityDefinitions);

        await _unitOfWork.SaveChangesAsync();
    }
}
