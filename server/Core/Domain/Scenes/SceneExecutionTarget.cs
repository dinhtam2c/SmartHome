using System.Text.Json;
using Core.Common;
using Core.Domain.Devices;

namespace Core.Domain.Scenes;

public class SceneExecutionTarget
{
    public Guid Id { get; private set; }
    public Guid SceneExecutionId { get; private set; }
    public Guid SceneTargetId { get; private set; }

    public Guid DeviceId { get; private set; }
    public string EndpointId { get; private set; }
    public string CapabilityId { get; private set; }
    public string DesiredStatePayload { get; private set; }
    public int Order { get; private set; }

    public SceneExecutionTargetStatus Status { get; private set; }
    public string? CommandCorrelationId { get; private set; }
    public string? UnresolvedDiffPayload { get; private set; }
    public string? Error { get; private set; }
    public long UpdatedAt { get; private set; }

    private SceneExecutionTarget()
    {
        EndpointId = string.Empty;
        CapabilityId = string.Empty;
        DesiredStatePayload = "{}";
    }

    private SceneExecutionTarget(Guid sceneExecutionId, SceneTarget target, int order, long now)
    {
        Id = Guid.NewGuid();
        SceneExecutionId = sceneExecutionId;
        SceneTargetId = target.Id;
        DeviceId = target.DeviceId;
        EndpointId = target.EndpointId;
        CapabilityId = target.CapabilityId;
        DesiredStatePayload = target.DesiredStatePayload;
        Order = order;

        Status = SceneExecutionTargetStatus.PendingEvaluation;
        UpdatedAt = now;
    }

    internal static SceneExecutionTarget SnapshotFrom(
        Guid sceneExecutionId,
        SceneTarget target,
        int order,
        long now)
    {
        return new SceneExecutionTarget(sceneExecutionId, target, order, now);
    }

    public Dictionary<string, object?> GetDesiredState()
    {
        if (string.IsNullOrWhiteSpace(DesiredStatePayload))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(DesiredStatePayload);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("DesiredState payload must be a JSON object.");
        }

        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            dictionary[property.Name] = JsonPayloadHelper.ConvertJsonElement(property.Value);
        }

        return dictionary;
    }

    internal void MarkSkipped(long? updatedAt = null)
    {
        Status = SceneExecutionTargetStatus.SkippedAlreadySatisfied;
        CommandCorrelationId = null;
        UnresolvedDiffPayload = null;
        Error = null;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkCommandPending(string correlationId, long? updatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("CorrelationId is required.", nameof(correlationId));

        Status = SceneExecutionTargetStatus.CommandPending;
        CommandCorrelationId = correlationId;
        UnresolvedDiffPayload = null;
        Error = null;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkLifecycle(CommandLifecycleStatus lifecycleStatus, string? error, long? updatedAt = null)
    {
        switch (lifecycleStatus)
        {
            case CommandLifecycleStatus.Accepted:
                Status = SceneExecutionTargetStatus.CommandAccepted;
                UnresolvedDiffPayload = null;
                Error = null;
                break;
            case CommandLifecycleStatus.Completed:
                Status = SceneExecutionTargetStatus.CommandCompleted;
                UnresolvedDiffPayload = null;
                Error = null;
                break;
            case CommandLifecycleStatus.Failed:
                Status = SceneExecutionTargetStatus.CommandFailed;
                UnresolvedDiffPayload = null;
                Error = string.IsNullOrWhiteSpace(error) ? "Command failed" : error;
                break;
            case CommandLifecycleStatus.TimedOut:
                Status = SceneExecutionTargetStatus.CommandTimedOut;
                UnresolvedDiffPayload = null;
                Error = string.IsNullOrWhiteSpace(error) ? "Command timed out" : error;
                break;
            default:
                Status = SceneExecutionTargetStatus.CommandPending;
                UnresolvedDiffPayload = null;
                Error = error;
                break;
        }

        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkFailed(SceneExecutionTargetStatus status, string? error, long? updatedAt = null)
    {
        if (!IsFailureStatus(status))
        {
            throw new InvalidOperationException($"Status '{status}' is not a failure status.");
        }

        Status = status;
        UnresolvedDiffPayload = null;
        Error = error;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkVerified(long? updatedAt = null)
    {
        Status = SceneExecutionTargetStatus.Verified;
        UnresolvedDiffPayload = null;
        Error = null;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkVerificationFailed(Dictionary<string, object?> unresolvedDiff, long? updatedAt = null)
    {
        Status = SceneExecutionTargetStatus.VerificationFailed;
        UnresolvedDiffPayload = JsonSerializer.Serialize(unresolvedDiff);
        Error = "Verification failed: current state does not match desired state.";
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    public Dictionary<string, object?>? GetUnresolvedDiff()
    {
        if (string.IsNullOrWhiteSpace(UnresolvedDiffPayload))
            return null;

        using var document = JsonDocument.Parse(UnresolvedDiffPayload);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return null;

        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            dictionary[property.Name] = JsonPayloadHelper.ConvertJsonElement(property.Value);
        }

        return dictionary;
    }

    private static bool IsFailureStatus(SceneExecutionTargetStatus status)
    {
        return status is
            SceneExecutionTargetStatus.DeviceNotFound
            or SceneExecutionTargetStatus.CapabilityNotFound
            or SceneExecutionTargetStatus.CapabilityAmbiguous
            or SceneExecutionTargetStatus.UnsupportedCapabilityRole
            or SceneExecutionTargetStatus.CommandGenerationFailed
            or SceneExecutionTargetStatus.CommandDispatchFailed
            or SceneExecutionTargetStatus.CommandFailed
            or SceneExecutionTargetStatus.CommandTimedOut
            or SceneExecutionTargetStatus.VerificationFailed;
    }

}

public enum SceneExecutionTargetStatus
{
    PendingEvaluation,
    SkippedAlreadySatisfied,
    CommandPending,
    CommandAccepted,
    CommandCompleted,
    Verified,
    VerificationFailed,

    DeviceNotFound,
    CapabilityNotFound,
    CapabilityAmbiguous,
    UnsupportedCapabilityRole,
    CommandGenerationFailed,
    CommandDispatchFailed,
    CommandFailed,
    CommandTimedOut
}
