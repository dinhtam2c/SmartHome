namespace Application.BusinessServices.ActionSets.Contracts;

public sealed record ActionSetView(
    IReadOnlyList<ActionSetActionView> Actions,
    ActionSetHooksView Hooks,
    ActionSetExecutionPolicyView ExecutionPolicy
);

public sealed record ActionSetHooksView(
    IReadOnlyList<ActionSetActionView> Before,
    IReadOnlyList<ActionSetActionView> OnSuccess,
    IReadOnlyList<ActionSetActionView> OnFailure
);

public sealed record ActionSetExecutionPolicyView(
    string Mode,
    bool ContinueOnError
);

public sealed record ActionTargetView(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId
);

public sealed record ActionSetActionView(
    Guid Id,
    string Type,
    ActionTargetView Target,
    IReadOnlyDictionary<string, object?>? State,
    string? Operation,
    IReadOnlyDictionary<string, object?>? Payload,
    int Order
);
