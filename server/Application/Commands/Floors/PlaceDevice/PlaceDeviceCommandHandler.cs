using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Devices;
using Core.Domain.Floors;
using MediatR;

namespace Application.Commands.Floors.PlaceDevice;

public sealed class PlaceDeviceCommandHandler : IRequestHandler<PlaceDeviceCommand, Guid>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PlaceDeviceCommandHandler(
        IFloorRepository floorRepository,
        IDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(PlaceDeviceCommand request, CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository,
            request.HomeId,
            request.FloorId,
            cancellationToken);

        var device = await _deviceRepository.GetById(request.DeviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        if (device.HomeId != request.HomeId)
            throw new DomainValidationException("Device does not belong to this home.");

        if (await _floorRepository.HasPlacedDevice(request.HomeId, request.DeviceId, cancellationToken))
            throw new DomainValidationException("Device is already placed on a floor.");

        var placedFloorDevice = FloorCommandHelper.Run(() =>
            floor.PlaceDevice(
                request.DeviceId,
                request.X,
                request.Y,
                request.FloorRoomId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return placedFloorDevice.Id;
    }
}
