using Application.BusinessServices.Devices.State;
using MediatR;

namespace Application.UseCases.Devices.Control.HandleCommandResult;

public sealed record HandleDeviceCommandResult(
    Guid DeviceId,
    string CapabilityId,
    string CorrelationId,
    string Operation,
    string Status,
    IReadOnlyList<CapabilityStateUpdate> StateChanges,
    string? Error,
    string EndpointId
) : IRequest;
