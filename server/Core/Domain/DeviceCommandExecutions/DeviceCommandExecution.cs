using Core.Common;
using Core.Domain.Devices;

namespace Core.Domain.DeviceCommandExecutions;

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

    private DeviceCommandExecution(Guid deviceId, string capabilityId, string endpointId, string correlationId,
        string operation, string? requestPayload, long? requestedAt = null)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        CapabilityId = capabilityId;
        EndpointId = endpointId;
        CorrelationId = correlationId;
        Operation = operation;
        Status = CommandLifecycleStatus.Pending;
        RequestPayload = requestPayload;
        RequestedAt = requestedAt ?? Time.UnixNow();
    }

    public static DeviceCommandExecution Create(
        Guid deviceId,
        string capabilityId,
        string endpointId,
        string correlationId,
        string operation,
        string? requestPayload,
        long? requestedAt = null)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new InvalidOperationException("CapabilityId must not be empty.");

        if (string.IsNullOrWhiteSpace(correlationId))
            throw new InvalidOperationException("CorrelationId must not be empty.");

        if (string.IsNullOrWhiteSpace(operation))
            throw new InvalidOperationException("Operation must not be empty.");

        return new DeviceCommandExecution(deviceId, capabilityId, endpointId, correlationId, operation, requestPayload, requestedAt);
    }

    public static bool TryParseStatus(string rawStatus, out CommandLifecycleStatus status)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            status = default;
            return false;
        }

        return Enum.TryParse(rawStatus, ignoreCase: true, out status)
            && Enum.IsDefined(typeof(CommandLifecycleStatus), status);
    }

    public void MarkAccepted(string? resultPayload = null)
    {
        ApplyLifecycleResult(CommandLifecycleStatus.Accepted, resultPayload, null);
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

    public void ApplyLifecycleResult(CommandLifecycleStatus status, string? resultPayload, string? error)
    {
        EnsureTransitionAllowed(status);

        Status = status;
        ResultPayload = resultPayload;
        Error = error;
    }

    private void EnsureTransitionAllowed(CommandLifecycleStatus nextStatus)
    {
        if (Status == CommandLifecycleStatus.Pending)
            return;

        if (Status == nextStatus)
            return;

        throw new InvalidOperationException(
            $"Command execution '{CorrelationId}' cannot transition from '{Status}' to '{nextStatus}'.");
    }
}
