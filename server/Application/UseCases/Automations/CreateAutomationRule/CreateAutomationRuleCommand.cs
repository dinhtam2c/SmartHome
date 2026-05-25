using Application.BusinessServices.Automations.Rules;
using Application.BusinessServices.ActionSets.Contracts;
using Domain.Models.Automations;
using MediatR;

namespace Application.UseCases.Automations.CreateAutomationRule;

public sealed record CreateAutomationRuleCommand(
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    AutomationConditionLogic ConditionLogic,
    int CooldownMs,
    IEnumerable<AutomationConditionModel>? Conditions,
    AutomationTimeWindowModel? TimeWindow,
    ActionSetInput? ActionSet
) : IRequest<Guid>;
