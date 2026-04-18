using Core.Domain.Devices;
using MediatR;

namespace Application.Queries.Devices.GetDeviceCommandExecutions;

public sealed record GetDeviceCommandExecutionsQuery(
    Guid DeviceId,

    // filters
    string? EndpointId,
    string? CapabilityId,
    string? CorrelationId,
    CommandLifecycleStatus? Status,
    long? FromRequestedAt,
    long? ToRequestedAt,

    // pagination
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<DeviceCommandExecutionDto>>;
