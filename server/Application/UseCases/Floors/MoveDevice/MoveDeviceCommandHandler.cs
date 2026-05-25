using Application.BusinessServices.Floors;
using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Devices;
using Domain.Models.Floors;
using MediatR;

namespace Application.UseCases.Floors.MoveDevice;

public sealed class MoveDeviceCommandHandler : IRequestHandler<MoveDeviceCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly FloorPlanConsistencyService _consistencyService;
    private readonly IUnitOfWork _unitOfWork;

    public MoveDeviceCommandHandler(
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

    public async Task Handle(MoveDeviceCommand request, CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository,
            request.HomeId,
            request.FloorId,
            cancellationToken);

        var placement = floor.DevicePlacements.FirstOrDefault(item => item.Id == request.PlacementId)
            ?? throw new FloorDevicePlacementNotFoundException(request.PlacementId);
        var device = await _deviceRepository.GetById(placement.DeviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(placement.DeviceId);

        FloorCommandHelper.Run(() => floor.MoveDevice(request.PlacementId, request.X, request.Y));
        FloorCommandHelper.Run(() =>
            _consistencyService.ApplyPlacementRoom(floor, device, request.X, request.Y));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
