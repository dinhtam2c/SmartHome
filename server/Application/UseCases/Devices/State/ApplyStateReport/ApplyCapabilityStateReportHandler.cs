using Application.Ports.Persistence;
using Application.BusinessServices.Devices.State;
using MediatR;

namespace Application.UseCases.Devices.State.ApplyStateReport;

public sealed class ApplyCapabilityStateReportHandler
    : IRequestHandler<ApplyCapabilityStateReport>
{
    private readonly ICapabilityStateUpdater _capabilityStateUpdater;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyCapabilityStateReportHandler(
        ICapabilityStateUpdater capabilityStateUpdater,
        IUnitOfWork unitOfWork
    )
    {
        _capabilityStateUpdater = capabilityStateUpdater;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ApplyCapabilityStateReport request, CancellationToken cancellationToken)
    {
        await _capabilityStateUpdater.Apply(
            request.DeviceId,
            request.StateChanges,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
