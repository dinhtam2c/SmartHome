namespace Application.Common.Errors;

public sealed class AutomationRuleNotFoundException : NotFoundException
{
    public AutomationRuleNotFoundException(Guid ruleId)
        : base($"Automation rule '{ruleId}' was not found.")
    {
    }
}
