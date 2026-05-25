using MediatR;

namespace Application.UseCases.Automations.GetAutomationRuleDetails;

public sealed record GetAutomationRuleDetailsQuery(Guid HomeId, Guid RuleId) : IRequest<AutomationRuleDetailsDto>;
