using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.ProvisionDevice;

public sealed record ProvisionDeviceCommand(
    string Name,
    string MacAddress,
    string FirmwareVersion,
    DeviceProtocol Protocol,
    IEnumerable<DeviceEndpointModel> Endpoints
) : IRequest;
