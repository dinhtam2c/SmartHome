using Core.Domain.ActionSets;

namespace Application.ActionSets;

public sealed record ActionSetModel(
    IEnumerable<ActionSetActionModel>? Actions,
    ActionSetHooksModel? Hooks,
    ActionSetExecutionPolicyModel? ExecutionPolicy
);

public sealed record ActionSetHooksModel(
    IEnumerable<ActionSetActionModel>? Before,
    IEnumerable<ActionSetActionModel>? OnSuccess,
    IEnumerable<ActionSetActionModel>? OnFailure
);

public sealed record ActionSetExecutionPolicyModel(
    string? Mode,
    bool? ContinueOnError
);

public sealed record ActionTargetModel(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId
);

public sealed record ActionSetActionModel(
    string Type,
    ActionTargetModel? Target,
    Dictionary<string, object?>? State,
    Dictionary<string, object?>? Options,
    string? Operation,
    Dictionary<string, object?>? Payload
);

public static class ActionSetWireNames
{
    public const string SetState = "setState";
    public const string InvokeOperation = "invokeOperation";
    public const string Main = "main";
    public const string Before = "before";
    public const string OnSuccess = "onSuccess";
    public const string OnFailure = "onFailure";
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

    public static string ToWireName(this ActionSetSection section)
    {
        return section switch
        {
            ActionSetSection.Main => Main,
            ActionSetSection.Before => Before,
            ActionSetSection.OnSuccess => OnSuccess,
            ActionSetSection.OnFailure => OnFailure,
            _ => section.ToString()
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

    public static string ToWireName(this ActionExecutionStatus status)
    {
        return status switch
        {
            ActionExecutionStatus.Pending => "pending",
            ActionExecutionStatus.SkippedAlreadySatisfied => "skippedAlreadySatisfied",
            ActionExecutionStatus.Skipped => "skipped",
            ActionExecutionStatus.CommandPending => "commandPending",
            ActionExecutionStatus.CommandAccepted => "commandAccepted",
            ActionExecutionStatus.Succeeded => "succeeded",
            ActionExecutionStatus.Failed => "failed",
            ActionExecutionStatus.TimedOut => "timedOut",
            ActionExecutionStatus.VerificationFailed => "verificationFailed",
            ActionExecutionStatus.DeviceNotFound => "deviceNotFound",
            ActionExecutionStatus.DeviceOffline => "deviceOffline",
            ActionExecutionStatus.CapabilityNotFound => "capabilityNotFound",
            ActionExecutionStatus.UnsupportedCapabilityRole => "unsupportedCapabilityRole",
            ActionExecutionStatus.CommandGenerationFailed => "commandGenerationFailed",
            ActionExecutionStatus.CommandDispatchFailed => "commandDispatchFailed",
            ActionExecutionStatus.CommandFailed => "commandFailed",
            _ => status.ToString()
        };
    }
}
