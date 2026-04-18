using Application.Queries.Devices.GetDeviceCommandExecutions;
using MediatR;

namespace Application.Queries.Devices.GetDeviceCapabilityStateHistory;

public sealed record GetDeviceCapabilityStateHistoryQuery(
    Guid DeviceId,

    // filters
    string? EndpointId,
    string? CapabilityId,
    long? FromReportedAt,
    long? ToReportedAt,

    // pagination
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<DeviceCapabilityStateHistoryDto>>;
