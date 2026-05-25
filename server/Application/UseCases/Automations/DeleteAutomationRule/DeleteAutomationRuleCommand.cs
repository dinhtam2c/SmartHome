using MediatR;

namespace Application.UseCases.Automations.DeleteAutomationRule;

public sealed record DeleteAutomationRuleCommand(Guid HomeId, Guid RuleId) : IRequest;
