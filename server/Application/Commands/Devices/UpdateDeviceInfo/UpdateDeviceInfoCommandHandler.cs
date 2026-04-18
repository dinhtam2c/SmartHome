using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.UpdateDeviceInfo;

public class UpdateDeviceInfoCommandHandler : IRequestHandler<UpdateDeviceInfoCommand>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDeviceInfoCommandHandler(
        IDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork
    )
    {
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateDeviceInfoCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetById(request.DeviceId)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        device.UpdateName(request.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
