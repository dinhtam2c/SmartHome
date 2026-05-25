using Application.BusinessServices.Floors;
using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Devices;
using Domain.Models.Floors;
using MediatR;

namespace Application.UseCases.Floors.PlaceDevice;

public sealed class PlaceDeviceCommandHandler : IRequestHandler<PlaceDeviceCommand, Guid>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly FloorPlanConsistencyService _consistencyService;
    private readonly IUnitOfWork _unitOfWork;

    public PlaceDeviceCommandHandler(
        IFloorRepository floorRepository,
        IDeviceRepository deviceRepository,
        FloorPlanConsistencyService consistencyService,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _deviceRepository = deviceRepository;
        _consistencyService = consistencyService;
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

        if (await _floorRepository.GetByDeviceId(request.DeviceId, cancellationToken) is not null)
            throw new ConflictException("Device is already placed on a floor.");

        var placedFloorDevice = FloorCommandHelper.Run(() =>
            floor.PlaceDevice(request.DeviceId, request.X, request.Y));

        FloorCommandHelper.Run(() =>
            _consistencyService.ApplyPlacementRoom(floor, device, request.X, request.Y));

        await FloorCommandHelper.SaveWithConflict(
            _unitOfWork,
            "Device is already placed on a floor.",
            cancellationToken);
        return placedFloorDevice.Id;
    }
}
