using Core.Domain.Automations;
using MediatR;

namespace Application.Commands.Automations.ExecuteAutomationRule;

public sealed record ExecuteAutomationRuleCommand(
    Guid HomeId,
    Guid RuleId,
    AutomationTriggerContext? Trigger
) : IRequest<Guid>;
