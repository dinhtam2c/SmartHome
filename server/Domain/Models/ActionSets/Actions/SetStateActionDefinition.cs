namespace Domain.Models.ActionSets;

public sealed record SetStateActionDefinition(
    ActionTarget Target,
    Dictionary<string, object?> State) : ActionDefinition(Target)
{
    public override ActionType Type => ActionType.SetState;
}
