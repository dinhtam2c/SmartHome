using MediatR;

namespace Application.Commands.Devices.SendDeviceCommand;

public sealed record SendDeviceCommandCommand(
    Guid DeviceId,
    string CapabilityId,
    string EndpointId,
    string Operation,
    object? Value,
    string? CorrelationId
) : IRequest;
