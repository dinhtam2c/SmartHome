using System.Text.Json;
using Core.Common;
using Core.Domain.ActionSets;
using Core.Domain.DeviceCommands;

namespace Core.Domain.Scenes;

public class SceneExecutionAction
{
    public Guid Id { get; private set; }
    public Guid SceneExecutionId { get; private set; }
    public Guid SceneActionId { get; private set; }
    public ActionSetSection Section { get; private set; }
    public ActionType Type { get; private set; }
    public Guid DeviceId { get; private set; }
    public string EndpointId { get; private set; }
    public string CapabilityId { get; private set; }
    public string? Operation { get; private set; }
    public string StatePayload { get; private set; }
    public string OptionsPayload { get; private set; }
    public string Payload { get; private set; }
    public int Order { get; private set; }
    public ActionExecutionStatus Status { get; private set; }
    public string? CommandCorrelationId { get; private set; }
    public string? UnresolvedDiffPayload { get; private set; }
    public string? Error { get; private set; }
    public long UpdatedAt { get; private set; }

    private SceneExecutionAction()
    {
        EndpointId = string.Empty;
        CapabilityId = string.Empty;
        StatePayload = "{}";
        OptionsPayload = "{}";
        Payload = "{}";
    }

    private SceneExecutionAction(Guid sceneExecutionId, SceneAction action, int order, long now)
    {
        Id = Guid.NewGuid();
        SceneExecutionId = sceneExecutionId;
        SceneActionId = action.Id;
        Section = action.Section;
        Type = action.Type;
        DeviceId = action.DeviceId;
        EndpointId = action.EndpointId;
        CapabilityId = action.CapabilityId;
        Operation = action.Operation;
        StatePayload = action.StatePayload;
        OptionsPayload = action.OptionsPayload;
        Payload = action.Payload;
        Order = order;
        Status = ActionExecutionStatus.Pending;
        UpdatedAt = now;
    }

    internal static SceneExecutionAction SnapshotFrom(
        Guid sceneExecutionId,
        SceneAction action,
        int order,
        long now)
    {
        return new SceneExecutionAction(sceneExecutionId, action, order, now);
    }

    public Dictionary<string, object?> GetState()
    {
        return JsonPayloadHelper.DeserializeDictionary(StatePayload, "Scene execution set-state action state");
    }

    public Dictionary<string, object?> GetOptions()
    {
        return JsonPayloadHelper.DeserializeDictionary(
            OptionsPayload,
            "Scene execution set-state action options",
            throwOnNonObject: false);
    }

    public Dictionary<string, object?> GetPayload()
    {
        return JsonPayloadHelper.DeserializeDictionary(
            Payload,
            "Scene execution invoke-operation action payload",
            throwOnNonObject: false);
    }

    public Dictionary<string, object?>? GetUnresolvedDiff()
    {
        if (string.IsNullOrWhiteSpace(UnresolvedDiffPayload))
            return null;

        return JsonPayloadHelper.DeserializeDictionary(
            UnresolvedDiffPayload,
            "Scene execution unresolved diff",
            throwOnNonObject: false);
    }

    internal void MarkSkippedAlreadySatisfied(long? updatedAt = null)
    {
        Status = ActionExecutionStatus.SkippedAlreadySatisfied;
        CommandCorrelationId = null;
        UnresolvedDiffPayload = null;
        Error = null;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkSkipped(string? reason = null, long? updatedAt = null)
    {
        Status = ActionExecutionStatus.Skipped;
        CommandCorrelationId = null;
        UnresolvedDiffPayload = null;
        Error = reason;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkCommandPending(string correlationId, long? updatedAt = null)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("CorrelationId is required.", nameof(correlationId));

        Status = ActionExecutionStatus.CommandPending;
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
                Status = ActionExecutionStatus.CommandAccepted;
                Error = null;
                break;
            case CommandLifecycleStatus.Completed:
                Status = ActionExecutionStatus.Succeeded;
                Error = null;
                break;
            case CommandLifecycleStatus.Failed:
                Status = ActionExecutionStatus.CommandFailed;
                Error = string.IsNullOrWhiteSpace(error) ? "Command failed" : error;
                break;
            case CommandLifecycleStatus.TimedOut:
                Status = ActionExecutionStatus.TimedOut;
                Error = string.IsNullOrWhiteSpace(error) ? "Command timed out" : error;
                break;
            default:
                Status = ActionExecutionStatus.CommandPending;
                Error = error;
                break;
        }

        UnresolvedDiffPayload = null;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkSucceeded(long? updatedAt = null)
    {
        Status = ActionExecutionStatus.Succeeded;
        UnresolvedDiffPayload = null;
        Error = null;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkFailed(
        ActionExecutionStatus status,
        string? error,
        string? correlationId = null,
        long? updatedAt = null)
    {
        if (!IsFailureStatus(status))
            throw new InvalidOperationException($"Status '{status}' is not a failure status.");

        Status = status;
        CommandCorrelationId = correlationId ?? CommandCorrelationId;
        UnresolvedDiffPayload = null;
        Error = error;
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    internal void MarkVerificationFailed(Dictionary<string, object?> unresolvedDiff, long? updatedAt = null)
    {
        Status = ActionExecutionStatus.VerificationFailed;
        UnresolvedDiffPayload = JsonSerializer.Serialize(unresolvedDiff);
        Error = "Verification failed: current state does not match desired state.";
        UpdatedAt = updatedAt ?? Time.UnixNow();
    }

    public static bool IsPendingStatus(ActionExecutionStatus status)
    {
        return status is
            ActionExecutionStatus.Pending
            or ActionExecutionStatus.CommandPending
            or ActionExecutionStatus.CommandAccepted;
    }

    public static bool IsSkippedStatus(ActionExecutionStatus status)
    {
        return status is
            ActionExecutionStatus.SkippedAlreadySatisfied
            or ActionExecutionStatus.Skipped;
    }

    public static bool IsSuccessfulStatus(ActionExecutionStatus status)
    {
        return status == ActionExecutionStatus.Succeeded;
    }

    public static bool IsFailureStatus(ActionExecutionStatus status)
    {
        return status is
            ActionExecutionStatus.Failed
            or ActionExecutionStatus.TimedOut
            or ActionExecutionStatus.VerificationFailed
            or ActionExecutionStatus.DeviceNotFound
            or ActionExecutionStatus.DeviceOffline
            or ActionExecutionStatus.CapabilityNotFound
            or ActionExecutionStatus.UnsupportedCapabilityRole
            or ActionExecutionStatus.CommandGenerationFailed
            or ActionExecutionStatus.CommandDispatchFailed
            or ActionExecutionStatus.CommandFailed;
    }
}
