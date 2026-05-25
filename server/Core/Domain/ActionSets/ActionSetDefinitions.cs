namespace Core.Domain.ActionSets;

public sealed record ActionSetDefinition(
    IReadOnlyList<ActionDefinition> Actions,
    ActionSetHooksDefinition Hooks,
    ActionSetExecutionPolicy ExecutionPolicy)
{
    public static ActionSetDefinition Empty { get; } = new(
        [],
        ActionSetHooksDefinition.Empty,
        ActionSetExecutionPolicy.Default);

    public bool IsEmpty => Actions.Count == 0;

    public IReadOnlyList<ActionDefinition> GetActions(ActionSetSection section)
    {
        return section switch
        {
            ActionSetSection.Main => Actions,
            ActionSetSection.Before => Hooks.Before,
            ActionSetSection.OnSuccess => Hooks.OnSuccess,
            ActionSetSection.OnFailure => Hooks.OnFailure,
            _ => []
        };
    }
}

public sealed record ActionSetHooksDefinition(
    IReadOnlyList<ActionDefinition> Before,
    IReadOnlyList<ActionDefinition> OnSuccess,
    IReadOnlyList<ActionDefinition> OnFailure)
{
    public static ActionSetHooksDefinition Empty { get; } = new([], [], []);
}

public sealed record ActionSetExecutionPolicy(
    ActionExecutionMode Mode,
    bool ContinueOnError)
{
    public static ActionSetExecutionPolicy Default { get; } = new(
        ActionExecutionMode.Sequential,
        ContinueOnError: false);
}

public sealed record ActionTarget(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId
);

public abstract record ActionDefinition(ActionTarget Target)
{
    public abstract ActionType Type { get; }
}

public sealed record SetStateActionDefinition(
    ActionTarget Target,
    Dictionary<string, object?> State,
    Dictionary<string, object?> Options) : ActionDefinition(Target)
{
    public override ActionType Type => ActionType.SetState;
}

public sealed record InvokeOperationActionDefinition(
    ActionTarget Target,
    string Operation,
    Dictionary<string, object?> Payload) : ActionDefinition(Target)
{
    public override ActionType Type => ActionType.InvokeOperation;
}

public enum ActionType
{
    SetState,
    InvokeOperation
}

public enum ActionSetSection
{
    Before = 0,
    Main = 1,
    OnSuccess = 2,
    OnFailure = 3
}

public enum ActionExecutionMode
{
    Sequential,
    Parallel
}

public enum ActionExecutionPhase
{
    BeforeHooks,
    MainActions,
    OnSuccessHooks,
    OnFailureHooks,
    Completed
}

public enum ActionExecutionStatus
{
    Pending,
    SkippedAlreadySatisfied,
    Skipped,
    CommandPending,
    CommandAccepted,
    Succeeded,
    Failed,
    TimedOut,
    VerificationFailed,
    DeviceNotFound,
    DeviceOffline,
    CapabilityNotFound,
    UnsupportedCapabilityRole,
    CommandGenerationFailed,
    CommandDispatchFailed,
    CommandFailed
}
