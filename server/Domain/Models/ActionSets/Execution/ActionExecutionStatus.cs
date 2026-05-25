namespace Domain.Models.ActionSets;

public enum ActionExecutionStatus
{
    Pending,
    WaitingForResult,
    Succeeded,
    Skipped,
    Failed
}
