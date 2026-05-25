using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Devices;
using MediatR;

namespace Application.UseCases.Devices.UpdateInfo;

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

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Device name is required.");

        device.UpdateName(request.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
