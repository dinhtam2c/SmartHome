using Application.Ports.Registries;
using Application.Ports.Messages;
using Application.BusinessServices.Capabilities.Validation;
using Domain.Models.DeviceCategories;
using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Devices;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Devices.Provisioning.RegisterDeviceManifest;

public sealed class RegisterDeviceManifestCommandHandler : IRequestHandler<RegisterDeviceManifestCommand>
{
    private readonly ILogger<RegisterDeviceManifestCommandHandler> _logger;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICapabilityRegistry _capabilityRegistry;
    private readonly ICapabilityProvisionValidator _capabilityRegistryValidator;
    private readonly IDeviceCategoryRegistry _deviceCategoryRegistry;
    private readonly IDeviceAccessManager _deviceAccessManager;

    public RegisterDeviceManifestCommandHandler(
        ILogger<RegisterDeviceManifestCommandHandler> logger,
        IDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork,
        ICapabilityRegistry capabilityRegistry,
        ICapabilityProvisionValidator capabilityRegistryValidator,
        IDeviceCategoryRegistry deviceCategoryRegistry,
        IDeviceAccessManager deviceAccessManager)
    {
        _deviceRepository = deviceRepository;
        _logger = logger;
        _capabilityRegistry = capabilityRegistry;
        _capabilityRegistryValidator = capabilityRegistryValidator;
        _deviceCategoryRegistry = deviceCategoryRegistry;
        _deviceAccessManager = deviceAccessManager;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RegisterDeviceManifestCommand request, CancellationToken cancellationToken)
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

        var validatedEndpoints = _capabilityRegistryValidator.NormalizeAndValidate(request.Endpoints);

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

        if (!string.IsNullOrEmpty(device.AccessToken))
        {
            await _deviceAccessManager.DeleteDeviceAccess(device.Id, cancellationToken);
        }

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
