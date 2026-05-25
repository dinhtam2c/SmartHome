using Application.BusinessServices.Devices.State;
using MediatR;

namespace Application.UseCases.Devices.State.ApplyStateReport;

public sealed record ApplyCapabilityStateReport(
    Guid DeviceId,
    IReadOnlyCollection<CapabilityStateUpdate> StateChanges
) : IRequest;
