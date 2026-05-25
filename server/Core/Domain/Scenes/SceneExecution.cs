using Core.Common;
using Core.Domain.ActionSets;
using Core.Domain.DeviceCommands;

namespace Core.Domain.Scenes;

public class SceneExecution
{
    public Guid Id { get; private set; }
    public Guid SceneId { get; private set; }
    public Guid HomeId { get; private set; }

    public string? TriggerSource { get; private set; }
    public SceneExecutionStatus Status { get; private set; }
    public ActionExecutionPhase Phase { get; private set; }
    public ActionExecutionMode ExecutionMode { get; private set; }
    public bool ContinueOnError { get; private set; }
    public bool FailureBranchSelected { get; private set; }
    public long StartedAt { get; private set; }
    public long? FinishedAt { get; private set; }

    public int TotalActions { get; private set; }
    public int PendingActions { get; private set; }
    public int SkippedActions { get; private set; }
    public int SuccessfulActions { get; private set; }
    public int FailedActions { get; private set; }

    private readonly List<SceneExecutionAction> _actions = [];
    public IReadOnlyCollection<SceneExecutionAction> Actions => _actions;

    private SceneExecution()
    {
    }

    private SceneExecution(Guid sceneId, Guid homeId, string? triggerSource, long startedAt)
    {
        Id = Guid.NewGuid();
        SceneId = sceneId;
        HomeId = homeId;
        TriggerSource = string.IsNullOrWhiteSpace(triggerSource) ? null : triggerSource.Trim();
        StartedAt = startedAt;
        Phase = ActionExecutionPhase.BeforeHooks;
        Status = SceneExecutionStatus.Running;
    }

    public static SceneExecution Start(Scene scene, string? triggerSource = null, long? startedAt = null)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var now = startedAt ?? Time.UnixNow();
        var execution = new SceneExecution(scene.Id, scene.HomeId, triggerSource, now)
        {
            ExecutionMode = scene.ExecutionMode,
            ContinueOnError = scene.ContinueOnError
        };

        foreach (var action in scene.Actions
                     .OrderBy(action => action.Section)
                     .ThenBy(action => action.Order))
        {
            execution._actions.Add(SceneExecutionAction.SnapshotFrom(
                execution.Id,
                action,
                action.Order,
                now));
        }

        execution.RecalculateStatus(now);
        return execution;
    }

    public void EnterPhase(ActionExecutionPhase phase, long? updatedAt = null)
    {
        if (phase == ActionExecutionPhase.OnFailureHooks)
            FailureBranchSelected = true;

        Phase = phase;
        RecalculateStatus(updatedAt);
    }

    public void Complete(long? updatedAt = null)
    {
        Phase = ActionExecutionPhase.Completed;
        RecalculateStatus(updatedAt);
    }

    public SceneExecutionAction? FindAction(Guid actionId)
    {
        return _actions.FirstOrDefault(action => action.Id == actionId);
    }

    public SceneExecutionAction? FindActionByCorrelation(Guid deviceId, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            return null;

        return _actions.FirstOrDefault(action =>
            action.DeviceId == deviceId
            && !string.IsNullOrWhiteSpace(action.CommandCorrelationId)
            && string.Equals(action.CommandCorrelationId, correlationId, StringComparison.OrdinalIgnoreCase));
    }

    public bool MarkActionSkippedAlreadySatisfied(Guid actionId, long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkSkippedAlreadySatisfied(updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkActionSkipped(Guid actionId, string? reason = null, long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkSkipped(reason, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkActionCommandPending(Guid actionId, string correlationId, long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkCommandPending(correlationId, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkActionSucceeded(Guid actionId, long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkSucceeded(updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkActionVerificationFailed(
        Guid actionId,
        Dictionary<string, object?> unresolvedDiff,
        long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkVerificationFailed(unresolvedDiff, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkActionFailed(
        Guid actionId,
        ActionExecutionStatus status,
        string? error,
        string? correlationId = null,
        long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkFailed(status, error, correlationId, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool TryApplyCommandLifecycle(
        Guid deviceId,
        string correlationId,
        CommandLifecycleStatus lifecycleStatus,
        string? error,
        long? updatedAt = null)
    {
        var action = FindActionByCorrelation(deviceId, correlationId);
        if (action is null)
            return false;

        action.MarkLifecycle(lifecycleStatus, error, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public IReadOnlyList<SceneExecutionAction> FindPendingActions(ActionSetSection section)
    {
        return _actions
            .Where(action =>
                action.Section == section
                && action.Status == ActionExecutionStatus.Pending
                && string.IsNullOrWhiteSpace(action.CommandCorrelationId))
            .OrderBy(action => action.Order)
            .ToList();
    }

    public bool HasActiveAction(ActionSetSection section)
    {
        return _actions.Any(action =>
            action.Section == section
            && action.Status is ActionExecutionStatus.CommandPending or ActionExecutionStatus.CommandAccepted);
    }

    public bool HasPendingAction(ActionSetSection section)
    {
        return _actions.Any(action =>
            action.Section == section
            && SceneExecutionAction.IsPendingStatus(action.Status));
    }

    public bool HasFailedAction(ActionSetSection section)
    {
        return _actions.Any(action =>
            action.Section == section
            && SceneExecutionAction.IsFailureStatus(action.Status));
    }

    public void SkipPendingActions(ActionSetSection section, string? reason = null, long? updatedAt = null)
    {
        foreach (var action in _actions.Where(action =>
                     action.Section == section
                     && action.Status == ActionExecutionStatus.Pending))
        {
            action.MarkSkipped(reason, updatedAt);
        }

        RecalculateStatus(updatedAt);
    }

    private void RecalculateStatus(long? now = null)
    {
        TotalActions = _actions.Count;
        PendingActions = _actions.Count(action => SceneExecutionAction.IsPendingStatus(action.Status));
        SkippedActions = _actions.Count(action => SceneExecutionAction.IsSkippedStatus(action.Status));
        SuccessfulActions = _actions.Count(action => SceneExecutionAction.IsSuccessfulStatus(action.Status));
        FailedActions = _actions.Count(action => SceneExecutionAction.IsFailureStatus(action.Status));

        if (Phase != ActionExecutionPhase.Completed)
        {
            Status = SceneExecutionStatus.Running;
            FinishedAt = null;
            return;
        }

        FinishedAt ??= now ?? Time.UnixNow();
        Status = FailureBranchSelected || FailedActions > 0
            ? SceneExecutionStatus.CompletedWithErrors
            : SceneExecutionStatus.Completed;
    }
}

public enum SceneExecutionStatus
{
    Running,
    Completed,
    CompletedWithErrors
}
