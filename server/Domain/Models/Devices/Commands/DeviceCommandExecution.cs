using Domain.Common;

namespace Domain.Models.Devices.Commands;

public class DeviceCommandExecution
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public string CapabilityId { get; set; } = string.Empty;
    public string EndpointId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public CommandLifecycleStatus Status { get; private set; }
    public string? RequestPayload { get; set; }
    public string? ResultPayload { get; set; }
    public string? Error { get; set; }
    public long RequestedAt { get; set; }

    private DeviceCommandExecution()
    {
    }

    private DeviceCommandExecution(Guid id, Guid deviceId, string capabilityId, string endpointId, string correlationId,
        string operation, string? requestPayload, long? requestedAt = null)
    {
        Id = id;
        DeviceId = deviceId;
        CapabilityId = capabilityId;
        EndpointId = endpointId;
        CorrelationId = correlationId;
        Operation = operation;
        Status = CommandLifecycleStatus.Pending;
        RequestPayload = requestPayload;
        RequestedAt = requestedAt ?? UnixTime.Now();
    }

    public static DeviceCommandExecution Create(
        Guid id,
        Guid deviceId,
        string capabilityId,
        string endpointId,
        string correlationId,
        string operation,
        string? requestPayload,
        long? requestedAt = null)
    {
        if (id == Guid.Empty)
            throw new InvalidOperationException("Id must not be empty.");

        if (string.IsNullOrWhiteSpace(endpointId))
            throw new InvalidOperationException("EndpointId must not be empty.");

        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new InvalidOperationException("CapabilityId must not be empty.");

        if (string.IsNullOrWhiteSpace(correlationId))
            throw new InvalidOperationException("CorrelationId must not be empty.");

        if (string.IsNullOrWhiteSpace(operation))
            throw new InvalidOperationException("Operation must not be empty.");

        return new DeviceCommandExecution(id, deviceId, capabilityId, endpointId, correlationId, operation, requestPayload, requestedAt);
    }

    public static bool TryParseResultStatus(string rawStatus, out CommandLifecycleStatus status)
    {
        if (string.Equals(rawStatus?.Trim(), "Completed", StringComparison.OrdinalIgnoreCase))
        {
            status = CommandLifecycleStatus.Completed;
            return true;
        }

        if (string.Equals(rawStatus?.Trim(), "Failed", StringComparison.OrdinalIgnoreCase))
        {
            status = CommandLifecycleStatus.Failed;
            return true;
        }

        status = default;
        return false;
    }

    public void MarkCompleted(string? resultPayload = null)
    {
        ApplyLifecycleResult(CommandLifecycleStatus.Completed, resultPayload, null);
    }

    public void MarkFailed(string error, string? resultPayload = null)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new InvalidOperationException("Error must not be empty for failed command execution.");

        ApplyLifecycleResult(CommandLifecycleStatus.Failed, resultPayload, error);
    }

    public void MarkTimedOut(string? error = null)
    {
        ApplyLifecycleResult(CommandLifecycleStatus.TimedOut, ResultPayload, error ?? "Command timed out");
    }

    private void ApplyLifecycleResult(CommandLifecycleStatus status, string? resultPayload, string? error)
    {
        EnsureTransitionAllowed(status);

        Status = status;
        ResultPayload = resultPayload;
        Error = error;
    }

    private void EnsureTransitionAllowed(CommandLifecycleStatus nextStatus)
    {
        if (Status == nextStatus)
            return;

        if (Status == CommandLifecycleStatus.Pending)
            return;

        throw new InvalidOperationException(
            $"Command execution '{CorrelationId}' cannot transition from '{Status}' to '{nextStatus}'.");
    }
}
