using MediatR;

namespace Application.UseCases.Automations.ExecuteAutomationRule;

public sealed record ExecuteAutomationRuleCommand(
    Guid HomeId,
    Guid RuleId
) : IRequest<Guid>;
