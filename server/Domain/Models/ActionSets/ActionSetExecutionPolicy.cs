namespace Domain.Models.ActionSets;

public sealed record ActionSetExecutionPolicy(
    ActionExecutionMode Mode,
    bool ContinueOnError)
{
    public static ActionSetExecutionPolicy Default { get; } = new(
        ActionExecutionMode.Sequential,
        ContinueOnError: false);
}
