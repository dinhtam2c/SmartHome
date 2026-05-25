namespace Presentation.ActionSets;

public sealed record ActionSetRequest(
    IEnumerable<ActionRequest>? Actions,
    ActionSetHooksRequest? Hooks,
    ActionSetExecutionPolicyRequest? ExecutionPolicy
);

public sealed record ActionSetHooksRequest(
    IEnumerable<ActionRequest>? Before,
    IEnumerable<ActionRequest>? OnSuccess,
    IEnumerable<ActionRequest>? OnFailure
);

public sealed record ActionSetExecutionPolicyRequest(
    string? Mode,
    bool? ContinueOnError
);

public sealed record ActionTargetRequest(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId
);

public sealed record ActionRequest(
    string Type,
    ActionTargetRequest? Target,
    Dictionary<string, object?>? State,
    string? Operation,
    Dictionary<string, object?>? Payload
);
