namespace Domain.Models.ActionSets;

public abstract record ActionDefinition(ActionTarget Target)
{
    public abstract ActionType Type { get; }
}
