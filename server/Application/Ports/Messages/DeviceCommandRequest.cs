namespace Application.Ports.Messages;

public sealed record DeviceCommandRequest(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId,
    string Operation,
    object? Value,
    string CorrelationId);
