using Application.Ports.Persistence;
using Application.Ports.Messages;
using Application.Common.Errors;
using Domain.Models.Devices;
using MediatR;
using Domain.Models.Floors;

namespace Application.UseCases.Devices.DeleteDevice;

public class DeleteDeviceCommandHandler : IRequestHandler<DeleteDeviceCommand>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFloorRepository _floorRepository;
    private readonly IDeviceAccessManager _deviceAccessManager;

    public DeleteDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        IFloorRepository floorRepository,
        IDeviceAccessManager deviceAccessManager,
        IUnitOfWork unitOfWork
    )
    {
        _deviceRepository = deviceRepository;
        _floorRepository = floorRepository;
        _deviceAccessManager = deviceAccessManager;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetById(request.DeviceId)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        await _deviceAccessManager.DeleteDeviceAccess(device.Id, cancellationToken);

        var floor = await _floorRepository.GetByDeviceId(device.Id, cancellationToken);
        floor?.RemoveDevicePlacementByDeviceId(device.Id);

        device.MarkDeleted();

        _deviceRepository.Remove(device);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
