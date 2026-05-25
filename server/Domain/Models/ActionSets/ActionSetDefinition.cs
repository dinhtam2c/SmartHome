namespace Domain.Models.ActionSets;

public sealed record ActionSetDefinition(
    IReadOnlyList<ActionDefinition> Actions,
    ActionSetHooksDefinition Hooks,
    ActionSetExecutionPolicy ExecutionPolicy);
