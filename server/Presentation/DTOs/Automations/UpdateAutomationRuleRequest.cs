using Domain.Models.Automations;
using Presentation.ActionSets;

namespace Presentation.Automations;

public sealed record UpdateAutomationRuleRequest(
    string? Name,
    string? Description,
    bool? IsEnabled,
    AutomationConditionLogic? ConditionLogic,
    int? CooldownMs,
    IEnumerable<AutomationConditionRequest>? Conditions,
    AutomationTimeWindowRequest? TimeWindow,
    ActionSetRequest? ActionSet
);
