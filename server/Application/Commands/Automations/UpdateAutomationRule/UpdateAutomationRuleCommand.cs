using Core.Domain.Automations;
using MediatR;

namespace Application.Commands.Automations.UpdateAutomationRule;

public sealed record UpdateAutomationRuleCommand(
    Guid HomeId,
    Guid RuleId,
    string? Name,
    string? Description,
    bool? IsEnabled,
    AutomationConditionLogic? ConditionLogic,
    int? CooldownMs,
    IEnumerable<AutomationConditionModel>? Conditions,
    AutomationTimeWindowModel? TimeWindow,
    ActionSetModel? ActionSet
) : IRequest;
