using Domain.Common;

namespace Infrastructure.Message.Mqtt.Dtos;

public record DeviceCommandMessage(
    string EndpointId,
    string CapabilityId,
    string Operation,
    object? Value,
    string CorrelationId,
    long RequestedAt
)
{
    public static DeviceCommandMessage Create(
        string endpointId,
        string capabilityId,
        string operation,
        object? value,
        string correlationId)
    {
        return new(
            EndpointId: endpointId,
            CapabilityId: capabilityId,
            Operation: operation,
            Value: value,
            CorrelationId: correlationId,
            RequestedAt: UnixTime.Now());
    }
}

public record DeviceCommandResultMessage(
    string CapabilityId,
    string CorrelationId,
    string Operation,
    string Status,
    IReadOnlyList<DeviceCapabilityStateMessage> StateChanges,
    string? Error,
    string EndpointId
);

public record DeviceAvailabilityMessage(
    string State
);

public record DeviceCapabilityStateMessage(
    string CapabilityId,
    string EndpointId,
    Dictionary<string, object?> State
);

public record DeviceSystemStateMessage(
    int Uptime
);
