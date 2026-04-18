using MediatR;

namespace Application.Commands.Devices.UpdateDeviceCommandResult;

public sealed record UpdateDeviceCommandResultCommand(
    Guid DeviceId,
    string CapabilityId,
    string CorrelationId,
    string Operation,
    string Status,
    object? Value,
    string? Error
) : IRequest;
