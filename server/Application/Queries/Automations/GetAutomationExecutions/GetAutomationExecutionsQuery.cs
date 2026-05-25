using Application.Queries.Devices.GetDeviceCommandExecutions;
using Core.Domain.Automations;
using MediatR;

namespace Application.Queries.Automations.GetAutomationExecutions;

public sealed record GetAutomationExecutionsQuery(
    Guid HomeId,
    Guid RuleId,
    AutomationExecutionStatus? Status,
    int Page,
    int PageSize
) : IRequest<PagedResult<AutomationExecutionListItemDto>>;
