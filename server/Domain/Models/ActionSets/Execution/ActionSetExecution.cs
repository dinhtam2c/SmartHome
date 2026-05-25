using Domain.Common;

namespace Domain.Models.ActionSets;

public sealed class ActionSetExecution
{
    public Guid Id { get; private set; }
    public ActionSetExecutionSource SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public Guid ActionSetId { get; private set; }
    public Guid HomeId { get; private set; }
    public ActionSetExecutionStatus Status { get; private set; }
    public ActionExecutionPhase Phase { get; private set; }
    public ActionExecutionMode ExecutionMode { get; private set; }
    public bool ContinueOnError { get; private set; }
    public long StartedAt { get; private set; }
    public long? FinishedAt { get; private set; }

    private readonly List<ActionSetActionExecution> _actions = [];
    public IReadOnlyCollection<ActionSetActionExecution> Actions => _actions;

    private ActionSetExecution()
    {
    }

    private ActionSetExecution(
        ActionSetExecutionSource sourceType,
        Guid sourceId,
        Guid actionSetId,
        Guid homeId,
        ActionExecutionMode executionMode,
        bool continueOnError,
        long startedAt)
    {
        Id = Guid.NewGuid();
        SourceType = sourceType;
        SourceId = sourceId;
        ActionSetId = actionSetId;
        HomeId = homeId;
        ExecutionMode = executionMode;
        ContinueOnError = executionMode == ActionExecutionMode.Sequential && continueOnError;
        StartedAt = startedAt;
        Phase = ActionExecutionPhase.BeforeHooks;
        Status = ActionSetExecutionStatus.Running;
    }

    public static ActionSetExecution Start(
        ActionSetExecutionSource sourceType,
        Guid sourceId,
        Guid actionSetId,
        Guid homeId,
        ActionExecutionMode executionMode,
        bool continueOnError,
        IEnumerable<ActionSetAction> actions,
        long? startedAt = null)
    {
        ArgumentNullException.ThrowIfNull(actions);

        var now = startedAt ?? UnixTime.Now();
        var execution = new ActionSetExecution(
            sourceType,
            sourceId,
            actionSetId,
            homeId,
            executionMode,
            continueOnError,
            now);

        foreach (var action in actions
                     .OrderBy(action => action.Section)
                     .ThenBy(action => action.Order))
        {
            execution._actions.Add(ActionSetActionExecution.SnapshotFrom(execution.Id, action));
        }

        execution.RecalculateStatus(now);
        return execution;
    }

    public void EnterPhase(ActionExecutionPhase phase, long? updatedAt = null)
    {
        Phase = phase;
        RecalculateStatus(updatedAt);
    }

    public void Complete(long? updatedAt = null)
    {
        Phase = ActionExecutionPhase.Completed;
        RecalculateStatus(updatedAt);
    }

    public ActionSetActionExecution? FindAction(Guid actionId)
    {
        return _actions.FirstOrDefault(action => action.Id == actionId);
    }

    public ActionSetActionExecution? FindActionByDeviceCommandExecutionId(Guid deviceCommandExecutionId)
    {
        if (deviceCommandExecutionId == Guid.Empty)
            return null;

        return _actions.FirstOrDefault(action =>
            action.DeviceCommandExecutionId == deviceCommandExecutionId);
    }

    public bool MarkActionSkipped(Guid actionId, string? reason = null, long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkSkipped(reason);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkActionWaitingForResult(Guid actionId, Guid deviceCommandExecutionId, long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkWaitingForResult(deviceCommandExecutionId);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkActionSucceeded(Guid actionId, long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkSucceeded();
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkActionFailed(
        Guid actionId,
        string? error,
        Guid? deviceCommandExecutionId = null,
        bool clearDeviceCommandExecutionId = false,
        long? updatedAt = null)
    {
        var action = FindAction(actionId);
        if (action is null)
            return false;

        action.MarkFailed(error, deviceCommandExecutionId, clearDeviceCommandExecutionId);
        RecalculateStatus(updatedAt);
        return true;
    }

    public IReadOnlyList<ActionSetActionExecution> FindPendingActions(ActionSetSection section)
    {
        return _actions
            .Where(action =>
                action.Section == section
                && action.Status == ActionExecutionStatus.Pending)
            .OrderBy(action => action.Order)
            .ToList();
    }

    public bool HasActionWaitingForResult(ActionSetSection section)
    {
        return _actions.Any(action =>
            action.Section == section
            && action.Status == ActionExecutionStatus.WaitingForResult);
    }

    public bool HasFailedAction(ActionSetSection section)
    {
        return _actions.Any(action =>
            action.Section == section
            && action.Status == ActionExecutionStatus.Failed);
    }

    public void SkipPendingActions(ActionSetSection section, string? reason = null, long? updatedAt = null)
    {
        foreach (var action in _actions.Where(action =>
                     action.Section == section
                     && action.Status == ActionExecutionStatus.Pending))
        {
            action.MarkSkipped(reason);
        }

        RecalculateStatus(updatedAt);
    }

    private void RecalculateStatus(long? now = null)
    {
        if (Phase != ActionExecutionPhase.Completed)
        {
            Status = ActionSetExecutionStatus.Running;
            FinishedAt = null;
            return;
        }

        FinishedAt ??= now ?? UnixTime.Now();
        Status = _actions.Any(action => action.Status == ActionExecutionStatus.Failed)
            ? ActionSetExecutionStatus.CompletedWithErrors
            : ActionSetExecutionStatus.Completed;
    }
}
