using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.Provisioning.RegisterDeviceManifest;

public sealed record RegisterDeviceManifestCommand(
    string Name,
    string Category,
    string MacAddress,
    string FirmwareVersion,
    DeviceProtocol Protocol,
    IEnumerable<DeviceEndpointModel> Endpoints
) : IRequest;
