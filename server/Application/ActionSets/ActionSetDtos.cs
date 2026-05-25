namespace Application.ActionSets;

public sealed record ActionSetDto(
    IReadOnlyList<ActionSetActionDto> Actions,
    ActionSetHooksDto Hooks,
    ActionSetExecutionPolicyDto ExecutionPolicy
);

public sealed record ActionSetHooksDto(
    IReadOnlyList<ActionSetActionDto> Before,
    IReadOnlyList<ActionSetActionDto> OnSuccess,
    IReadOnlyList<ActionSetActionDto> OnFailure
);

public sealed record ActionSetExecutionPolicyDto(
    string Mode,
    bool ContinueOnError
);

public sealed record ActionTargetDto(
    Guid DeviceId,
    string EndpointId,
    string CapabilityId
);

public sealed record ActionSetActionDto(
    Guid Id,
    string Type,
    ActionTargetDto Target,
    Dictionary<string, object?>? State,
    Dictionary<string, object?>? Options,
    string? Operation,
    Dictionary<string, object?>? Payload,
    int Order
);

public sealed record ActionExecutionDto(
    Guid Id,
    Guid DefinitionId,
    string Section,
    string Type,
    ActionTargetDto Target,
    Dictionary<string, object?>? State,
    Dictionary<string, object?>? Options,
    string? Operation,
    Dictionary<string, object?>? Payload,
    string Status,
    string? CommandCorrelationId,
    Dictionary<string, object?>? UnresolvedDiff,
    string? Error,
    int Order,
    long UpdatedAt
);
