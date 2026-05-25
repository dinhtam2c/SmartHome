using MediatR;

namespace Application.UseCases.Automations.GetAutomationRules;

public sealed record GetAutomationRulesQuery(Guid HomeId) : IRequest<IReadOnlyList<AutomationRuleListItemDto>>;
