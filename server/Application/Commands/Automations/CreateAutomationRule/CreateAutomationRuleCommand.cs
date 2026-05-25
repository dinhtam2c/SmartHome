using Core.Domain.Automations;
using MediatR;

namespace Application.Commands.Automations.CreateAutomationRule;

public sealed record CreateAutomationRuleCommand(
    Guid HomeId,
    string Name,
    string? Description,
    bool IsEnabled,
    AutomationConditionLogic ConditionLogic,
    int CooldownMs,
    IEnumerable<AutomationConditionModel>? Conditions,
    AutomationTimeWindowModel? TimeWindow,
    ActionSetModel? ActionSet
) : IRequest<Guid>;
