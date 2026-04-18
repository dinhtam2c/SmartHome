using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.UpdateDeviceAvailability;

public class UpdateDeviceAvailabilityCommandHandler : IRequestHandler<UpdateDeviceAvailabilityCommand>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDeviceAvailabilityCommandHandler(
        IDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork
    )
    {
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateDeviceAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetById(request.DeviceId)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        switch (request.State)
        {
            case "Online":
                device.MarkOnline();
                break;
            case "Offline":
                device.MarkOffline();
                break;
            default:
                throw new InvalidStateException(request.DeviceId, request.State);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
