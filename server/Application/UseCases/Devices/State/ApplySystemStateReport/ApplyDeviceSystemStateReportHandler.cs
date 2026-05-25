using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Devices;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Devices.State.ApplySystemStateReport;

public sealed class ApplyDeviceSystemStateReportHandler : IRequestHandler<ApplyDeviceSystemStateReport>
{
    private readonly ILogger<ApplyDeviceSystemStateReportHandler> _logger;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyDeviceSystemStateReportHandler(
        ILogger<ApplyDeviceSystemStateReportHandler> logger,
        IDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork
    )
    {
        _logger = logger;
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ApplyDeviceSystemStateReport request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetById(request.DeviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        if (!device.IsOnline)
        {
            _logger.LogWarning("Device {DeviceId} is offline, ignoring system state update", request.DeviceId);
            return;
        }

        device.UpdateSystemState(request.Uptime);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Device {DeviceId} system state updated: Uptime={Uptime}",
            request.DeviceId,
            request.Uptime
        );
    }
}
