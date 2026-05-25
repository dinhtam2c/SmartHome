using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Floors;
using MediatR;

namespace Application.Commands.Floors.RemovePlacedFloorDevice;

public sealed class RemovePlacedFloorDeviceCommandHandler : IRequestHandler<RemovePlacedFloorDeviceCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemovePlacedFloorDeviceCommandHandler(
        IFloorRepository floorRepository,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemovePlacedFloorDeviceCommand request, CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository,
            request.HomeId,
            request.FloorId,
            cancellationToken);

        FloorCommandHelper.Run(() => floor.RemovePlacedFloorDevice(request.PlacedFloorDeviceId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
