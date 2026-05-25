using Application.BusinessServices.Automations.Rules;
using Application.BusinessServices.ActionSets.Contracts;
using Domain.Models.Automations;
using MediatR;

namespace Application.UseCases.Automations.UpdateAutomationRule;

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
    ActionSetInput? ActionSet
) : IRequest;
