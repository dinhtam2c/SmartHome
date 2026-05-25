namespace Application.Commands.Devices.SendDeviceCommand;

public sealed record DeviceCommandModel(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    string Operation,
    object? Value,
    string? CorrelationId);
