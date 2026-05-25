namespace Domain.Models.ActionSets;

public enum ActionExecutionPhase
{
    BeforeHooks,
    MainActions,
    OnSuccessHooks,
    OnFailureHooks,
    Completed
}
