using MediatR;

namespace Application.Queries.Automations.GetAutomationRuleDetails;

public sealed record GetAutomationRuleDetailsQuery(Guid HomeId, Guid RuleId) : IRequest<AutomationRuleDetailsDto>;
