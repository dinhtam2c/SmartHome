namespace Domain.Models.ActionSets;

public sealed record ActionSetHooksDefinition(
    IReadOnlyList<ActionDefinition> Before,
    IReadOnlyList<ActionDefinition> OnSuccess,
    IReadOnlyList<ActionDefinition> OnFailure)
{
    public static ActionSetHooksDefinition Empty { get; } = new([], [], []);
}
