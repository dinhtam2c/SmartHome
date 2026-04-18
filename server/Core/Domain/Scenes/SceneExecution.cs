using Core.Common;
using Core.Domain.Devices;

namespace Core.Domain.Scenes;

public class SceneExecution
{
    public Guid Id { get; private set; }
    public Guid SceneId { get; private set; }
    public Guid HomeId { get; private set; }

    public string? TriggerSource { get; private set; }
    public SceneExecutionStatus Status { get; private set; }
    public long StartedAt { get; private set; }
    public long? FinishedAt { get; private set; }

    public int TotalTargets { get; private set; }
    public int PendingTargets { get; private set; }
    public int SkippedTargets { get; private set; }
    public int SuccessfulTargets { get; private set; }
    public int FailedTargets { get; private set; }
    public int FailedSideEffects => _sideEffects.Count(sideEffect =>
        sideEffect.Status == SceneExecutionSideEffectStatus.Failed);

    private readonly List<SceneExecutionTarget> _targets = [];
    public IReadOnlyCollection<SceneExecutionTarget> Targets => _targets;

    private readonly List<SceneExecutionSideEffect> _sideEffects = [];
    public IReadOnlyCollection<SceneExecutionSideEffect> SideEffects => _sideEffects;

    private SceneExecution()
    {
    }

    private SceneExecution(Guid sceneId, Guid homeId, string? triggerSource, long startedAt)
    {
        Id = Guid.NewGuid();
        SceneId = sceneId;
        HomeId = homeId;
        TriggerSource = triggerSource;
        StartedAt = startedAt;
        Status = SceneExecutionStatus.Running;
    }

    public static SceneExecution Start(Scene scene, string? triggerSource = null, long? startedAt = null)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var now = startedAt ?? Time.UnixNow();
        var execution = new SceneExecution(scene.Id, scene.HomeId, triggerSource, now);

        foreach (var target in scene.Targets.OrderBy(target => target.Order))
        {
            execution._targets.Add(SceneExecutionTarget.SnapshotFrom(
                execution.Id,
                target,
                target.Order,
                now));
        }

        foreach (var sideEffect in scene.SideEffects.OrderBy(effect => effect.Order))
        {
            execution._sideEffects.Add(SceneExecutionSideEffect.SnapshotFrom(
                execution.Id,
                sideEffect,
                sideEffect.Order,
                now));
        }

        execution.RecalculateStatus(now);
        return execution;
    }

    public bool MarkTargetSkipped(Guid executionTargetId, long? updatedAt = null)
    {
        var target = FindTarget(executionTargetId);
        if (target is null)
            return false;

        target.MarkSkipped(updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkTargetCommandPending(Guid executionTargetId, string correlationId, long? updatedAt = null)
    {
        var target = FindTarget(executionTargetId);
        if (target is null)
            return false;

        target.MarkCommandPending(correlationId, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkTargetVerified(Guid executionTargetId, long? updatedAt = null)
    {
        var target = FindTarget(executionTargetId);
        if (target is null)
            return false;

        target.MarkVerified(updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkTargetVerificationFailed(
        Guid executionTargetId,
        Dictionary<string, object?> unresolvedDiff,
        long? updatedAt = null)
    {
        var target = FindTarget(executionTargetId);
        if (target is null)
            return false;

        target.MarkVerificationFailed(unresolvedDiff, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkTargetFailed(
        Guid executionTargetId,
        SceneExecutionTargetStatus status,
        string? error,
        long? updatedAt = null)
    {
        var target = FindTarget(executionTargetId);
        if (target is null)
            return false;

        target.MarkFailed(status, error, updatedAt);
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
        var target = _targets.FirstOrDefault(item =>
            item.DeviceId == deviceId
            && !string.IsNullOrWhiteSpace(item.CommandCorrelationId)
            && string.Equals(item.CommandCorrelationId, correlationId, StringComparison.OrdinalIgnoreCase));

        if (target is null)
            return false;

        target.MarkLifecycle(lifecycleStatus, error, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public SceneExecutionTarget? FindTarget(Guid targetId)
    {
        return _targets.FirstOrDefault(target => target.Id == targetId);
    }

    public SceneExecutionTarget? FindTargetByCorrelation(Guid deviceId, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
            return null;

        return _targets.FirstOrDefault(target =>
            target.DeviceId == deviceId
            && !string.IsNullOrWhiteSpace(target.CommandCorrelationId)
            && string.Equals(target.CommandCorrelationId, correlationId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<SceneExecutionSideEffect> FindPendingSideEffects(SceneSideEffectTiming timing)
    {
        return _sideEffects
            .Where(sideEffect => sideEffect.Timing == timing && sideEffect.Status == SceneExecutionSideEffectStatus.Pending)
            .OrderBy(sideEffect => sideEffect.Order)
            .ToList();
    }

    public bool MarkSideEffectSucceeded(Guid executionSideEffectId, string? correlationId, long? updatedAt = null)
    {
        var sideEffect = FindSideEffect(executionSideEffectId);
        if (sideEffect is null)
            return false;

        sideEffect.MarkSucceeded(correlationId, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkSideEffectFailed(
        Guid executionSideEffectId,
        string? error,
        string? correlationId = null,
        long? updatedAt = null)
    {
        var sideEffect = FindSideEffect(executionSideEffectId);
        if (sideEffect is null)
            return false;

        sideEffect.MarkFailed(error, correlationId, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public bool MarkSideEffectSkipped(Guid executionSideEffectId, string? reason = null, long? updatedAt = null)
    {
        var sideEffect = FindSideEffect(executionSideEffectId);
        if (sideEffect is null)
            return false;

        sideEffect.MarkSkipped(reason, updatedAt);
        RecalculateStatus(updatedAt);
        return true;
    }

    public SceneExecutionSideEffect? FindSideEffect(Guid sideEffectId)
    {
        return _sideEffects.FirstOrDefault(sideEffect => sideEffect.Id == sideEffectId);
    }

    private void RecalculateStatus(long? now = null)
    {
        TotalTargets = _targets.Count;
        PendingTargets = _targets.Count(target => IsPendingStatus(target.Status));
        SkippedTargets = _targets.Count(target => target.Status == SceneExecutionTargetStatus.SkippedAlreadySatisfied);
        SuccessfulTargets = _targets.Count(target => target.Status is
            SceneExecutionTargetStatus.CommandCompleted
            or SceneExecutionTargetStatus.Verified);
        FailedTargets = _targets.Count(target => IsFailureStatus(target.Status));
        var pendingSideEffects = _sideEffects.Count(sideEffect =>
            sideEffect.Status == SceneExecutionSideEffectStatus.Pending);

        if (PendingTargets > 0 || pendingSideEffects > 0)
        {
            Status = SceneExecutionStatus.Running;
            FinishedAt = null;
            return;
        }

        FinishedAt ??= now ?? Time.UnixNow();

        if (FailedTargets > 0)
        {
            Status = SceneExecutionStatus.CompletedWithErrors;
            return;
        }

        Status = SceneExecutionStatus.Completed;
    }

    private static bool IsPendingStatus(SceneExecutionTargetStatus status)
    {
        return status is
            SceneExecutionTargetStatus.PendingEvaluation
            or SceneExecutionTargetStatus.CommandPending
            or SceneExecutionTargetStatus.CommandAccepted;
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
            or SceneExecutionTargetStatus.CommandTimedOut;
    }
}

public enum SceneExecutionStatus
{
    Running,
    Completed,
    CompletedWithErrors
}
