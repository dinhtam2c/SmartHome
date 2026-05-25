using Domain.Models.ActionSets;

namespace Application.BusinessServices.ActionSets.Contracts;

public sealed record ActionSetInput(
    IEnumerable<ActionSetActionInput>? Actions,
    ActionSetHooksInput? Hooks,
    ActionSetExecutionPolicyInput? ExecutionPolicy
);

public sealed record ActionSetHooksInput(
    IEnumerable<ActionSetActionInput>? Before,
    IEnumerable<ActionSetActionInput>? OnSuccess,
    IEnumerable<ActionSetActionInput>? OnFailure
);

public sealed record ActionSetExecutionPolicyInput(
    string? Mode,
    bool? ContinueOnError
);

public sealed record ActionTargetInput(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId
);

public sealed record ActionSetActionInput(
    string Type,
    ActionTargetInput? Target,
    Dictionary<string, object?>? State,
    string? Operation,
    Dictionary<string, object?>? Payload
);

public static class ActionSetWireNames
{
    public const string SetState = "setState";
    public const string InvokeOperation = "invokeOperation";
    public const string Sequential = "sequential";
    public const string Parallel = "parallel";

    public static string ToWireName(this ActionType type)
    {
        return type switch
        {
            ActionType.SetState => SetState,
            ActionType.InvokeOperation => InvokeOperation,
            _ => type.ToString()
        };
    }

    public static string ToWireName(this ActionExecutionMode mode)
    {
        return mode switch
        {
            ActionExecutionMode.Sequential => Sequential,
            ActionExecutionMode.Parallel => Parallel,
            _ => mode.ToString()
        };
    }

    public static string ToWireName(this ActionExecutionPhase phase)
    {
        return phase switch
        {
            ActionExecutionPhase.BeforeHooks => "beforeHooks",
            ActionExecutionPhase.MainActions => "mainActions",
            ActionExecutionPhase.OnSuccessHooks => "onSuccessHooks",
            ActionExecutionPhase.OnFailureHooks => "onFailureHooks",
            ActionExecutionPhase.Completed => "completed",
            _ => phase.ToString()
        };
    }

}
