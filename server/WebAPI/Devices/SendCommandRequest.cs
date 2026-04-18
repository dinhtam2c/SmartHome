public record SendCommandRequest(
    string CapabilityId,
    string EndpointId,
    string Operation,
    object? Value,
    string? CorrelationId
);
