using Application.Common.Data;
using Application.Exceptions;
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
    private readonly ICapabilityProvisionValidator _capabilityRegistryValidator;
    private readonly IDeviceCategoryRegistry _deviceCategoryRegistry;

    public ProvisionDeviceCommandHandler(
        ILogger<ProvisionDeviceCommandHandler> logger,
        IDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityProvisionValidator capabilityRegistryValidator,
        IDeviceCategoryRegistry deviceCategoryRegistry)
    {
        _deviceRepository = deviceRepository;
        _logger = logger;
        _capabilityRegistry = capabilityRegistry;
        _capabilityRegistryValidator = capabilityRegistryValidator;
        _deviceCategoryRegistry = deviceCategoryRegistry;
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

        var category = DeviceCategoryIds.Normalize(request.Category);
        if (!_deviceCategoryRegistry.TryGetDefinition(category, out _))
        {
            throw new InvalidCapabilityProvisionException(
                $"Device category '{request.Category}' is not found in device category registry");
        }

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
            category,
            request.FirmwareVersion,
            request.Protocol,
            endpoints,
            capabilityDefinitions);

        await _unitOfWork.SaveChangesAsync();
    }
}
