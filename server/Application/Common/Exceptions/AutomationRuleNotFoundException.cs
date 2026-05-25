namespace Application.Exceptions;

public sealed class AutomationRuleNotFoundException : NotFoundException
{
    public AutomationRuleNotFoundException(Guid ruleId)
        : base($"Automation rule '{ruleId}' was not found.")
    {
    }
}

public sealed class AutomationExecutionNotFoundException : NotFoundException
{
    public AutomationExecutionNotFoundException(Guid executionId)
        : base($"Automation execution '{executionId}' was not found.")
    {
    }
}
