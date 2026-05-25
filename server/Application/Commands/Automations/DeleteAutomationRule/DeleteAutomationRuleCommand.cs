using MediatR;

namespace Application.Commands.Automations.DeleteAutomationRule;

public sealed record DeleteAutomationRuleCommand(Guid HomeId, Guid RuleId) : IRequest;
