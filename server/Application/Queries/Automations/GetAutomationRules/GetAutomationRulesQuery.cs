using MediatR;

namespace Application.Queries.Automations.GetAutomationRules;

public sealed record GetAutomationRulesQuery(Guid HomeId) : IRequest<IReadOnlyList<AutomationRuleListItemDto>>;
