using MediatR;

namespace Application.Queries.Automations.GetAutomationExecutionDetails;

public sealed record GetAutomationExecutionDetailsQuery(
    Guid HomeId,
    Guid RuleId,
    Guid ExecutionId
) : IRequest<AutomationExecutionDetailsDto>;
