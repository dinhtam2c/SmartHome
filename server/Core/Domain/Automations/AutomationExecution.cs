using System.Text.Json;
using Core.Common;
using Core.Domain.ActionSets;
using Core.Domain.DeviceCommands;

namespace Core.Domain.Automations;

public class AutomationExecution
{
    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public Guid HomeId { get; private set; }

    public Guid? TriggerDeviceId { get; private set; }
    public string? TriggerEndpointId { get; private set; }
    public string? TriggerCapabilityId { get; private set; }
    public string? TriggerStatePayload { get; private set; }
    public string? TriggerSource { get; private set; }

    public AutomationExecutionStatus Status { get; private set; }
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

    private readonly List<AutomationExecutionAction> _actions = [];
    public IReadOnlyCollection<AutomationExecutionAction> Actions => _actions;

    private AutomationExecution()
    {
    }

    private AutomationExecution(
        Guid ruleId,
        Guid homeId,
        AutomationTriggerContext? trigger,
        long startedAt)
    {
        Id = Guid.NewGuid();
        RuleId = ruleId;
        HomeId = homeId;
        TriggerDeviceId = trigger?.TriggerDeviceId;
        TriggerEndpointId = string.IsNullOrWhiteSpace(trigger?.TriggerEndpointId)
            ? null
            : trigger.TriggerEndpointId.Trim();
        TriggerCapabilityId = string.IsNullOrWhiteSpace(trigger?.TriggerCapabilityId)
            ? null
            : trigger.TriggerCapabilityId.Trim();
        TriggerStatePayload = trigger?.TriggerState is null
            ? null
            : JsonSerializer.Serialize(trigger.TriggerState);
        TriggerSource = string.IsNullOrWhiteSpace(trigger?.TriggerSource)
            ? null
            : trigger.TriggerSource.Trim();
        StartedAt = startedAt;
        Phase = ActionExecutionPhase.BeforeHooks;
        Status = AutomationExecutionStatus.Running;
    }

    public static AutomationExecution Start(
        AutomationRule rule,
        AutomationTriggerContext? trigger = null,
        long? startedAt = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var now = startedAt ?? Time.UnixNow();
        var execution = new AutomationExecution(rule.Id, rule.HomeId, trigger, now)
        {
            ExecutionMode = rule.ExecutionMode,
            ContinueOnError = rule.ContinueOnError
        };

        foreach (var action in rule.Actions
                     .OrderBy(action => action.Section)
                     .ThenBy(action => action.Order))
        {
            execution._actions.Add(AutomationExecutionAction.SnapshotFrom(
                execution.Id,
                action,
                action.Order,
                now));
        }

        execution.RecalculateStatus(now);
        return execution;
    }

    public Dictionary<string, object?>? GetTriggerState()
    {
        if (string.IsNullOrWhiteSpace(TriggerStatePayload))
            return null;

        return JsonPayloadHelper.DeserializeDictionary(
            TriggerStatePayload,
            "Automation trigger state",
            throwOnNonObject: false);
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

    public AutomationExecutionAction? FindAction(Guid actionId)
    {
        return _actions.FirstOrDefault(action => action.Id == actionId);
    }

    public AutomationExecutionAction? FindActionByCorrelation(Guid deviceId, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            return null;

        return _actions.FirstOrDefault(action =>
            action.DeviceId == deviceId
            && !string.IsNullOrWhiteSpace(action.CommandCorrelationId)
            && action.CommandCorrelationId.Equals(correlationId, StringComparison.OrdinalIgnoreCase));
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

    public IReadOnlyList<AutomationExecutionAction> FindPendingActions(ActionSetSection section)
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
            && AutomationExecutionAction.IsPendingStatus(action.Status));
    }

    public bool HasFailedAction(ActionSetSection section)
    {
        return _actions.Any(action =>
            action.Section == section
            && AutomationExecutionAction.IsFailureStatus(action.Status));
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
        PendingActions = _actions.Count(action => AutomationExecutionAction.IsPendingStatus(action.Status));
        SkippedActions = _actions.Count(action => AutomationExecutionAction.IsSkippedStatus(action.Status));
        SuccessfulActions = _actions.Count(action => AutomationExecutionAction.IsSuccessfulStatus(action.Status));
        FailedActions = _actions.Count(action => AutomationExecutionAction.IsFailureStatus(action.Status));

        if (Phase != ActionExecutionPhase.Completed)
        {
            Status = AutomationExecutionStatus.Running;
            FinishedAt = null;
            return;
        }

        FinishedAt ??= now ?? Time.UnixNow();
        Status = FailureBranchSelected || FailedActions > 0
            ? AutomationExecutionStatus.CompletedWithErrors
            : AutomationExecutionStatus.Completed;
    }
}

public enum AutomationExecutionStatus
{
    Running,
    Completed,
    CompletedWithErrors
}
