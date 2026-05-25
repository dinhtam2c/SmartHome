using Core.Domain.Devices;

namespace Application.Queries.Devices.GetDeviceCommandExecutions;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public sealed record DeviceCommandExecutionDto(
    Guid Id,
    Guid DeviceId,
    string CapabilityId,
    string EndpointId,
    string CorrelationId,
    string Operation,
    CommandLifecycleStatus Status,
    string? RequestPayload,
    string? ResultPayload,
    string? Error,
    long RequestedAt
);
