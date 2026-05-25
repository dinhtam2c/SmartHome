using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DeleteDevice;

public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork
    )
    {
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetById(request.DeviceId)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        device.MarkDeleted();

        _deviceRepository.Remove(device);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
