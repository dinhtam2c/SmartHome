using Core.Domain.Automations;
using WebAPI.ActionSets;

namespace WebAPI.Automations;

public sealed record AddAutomationRuleRequest(
    string Name,
    string? Description,
    bool IsEnabled,
    AutomationConditionLogic ConditionLogic,
    int CooldownMs,
    IEnumerable<AutomationConditionRequest>? Conditions,
    AutomationTimeWindowRequest? TimeWindow,
    ActionSetRequest? ActionSet
);
