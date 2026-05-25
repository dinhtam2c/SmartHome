namespace Domain.Models.ActionSets;

public sealed record InvokeOperationActionDefinition(
    ActionTarget Target,
    string Operation,
    Dictionary<string, object?> Payload) : ActionDefinition(Target)
{
    public override ActionType Type => ActionType.InvokeOperation;
}
