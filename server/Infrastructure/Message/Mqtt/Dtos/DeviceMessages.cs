using Core.Common;

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
    public static DeviceCommandMessage New(string endpointId, string capabilityId, string operation,
        object? value, string? correlationId = null)
    {
        return new(
            EndpointId: endpointId,
            CapabilityId: capabilityId,
            Operation: operation,
            Value: value,
            CorrelationId: correlationId ?? Guid.NewGuid().ToString("N"),
            RequestedAt: Time.UnixNow());
    }
}

public record DeviceCommandResultMessage(
    string CapabilityId,
    string CorrelationId,
    string Operation,
    string Status,
    object? Value,
    string? Error
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
