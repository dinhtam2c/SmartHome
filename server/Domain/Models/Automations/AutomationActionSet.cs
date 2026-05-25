using Domain.Models.ActionSets;

namespace Domain.Models.Automations;

public sealed class AutomationActionSet : ActionSet
{
    public Guid RuleId { get; private set; }

    protected override bool RequiresMainAction => true;

    private AutomationActionSet()
    {
    }

    private AutomationActionSet(Guid ruleId, ActionSetDefinition definition) : base(definition)
    {
        if (ruleId == Guid.Empty)
            throw new ArgumentException("RuleId is required.", nameof(ruleId));

        RuleId = ruleId;
    }

    public static AutomationActionSet Create(Guid ruleId, ActionSetDefinition definition)
    {
        return new AutomationActionSet(ruleId, definition);
    }
}
