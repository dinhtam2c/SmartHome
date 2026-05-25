using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Floors;
using MediatR;

namespace Application.Commands.Floors.MoveDevice;

public sealed class MoveDeviceCommandHandler : IRequestHandler<MoveDeviceCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MoveDeviceCommandHandler(
        IFloorRepository floorRepository,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MoveDeviceCommand request, CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository,
            request.HomeId,
            request.FloorId,
            cancellationToken);

        FloorCommandHelper.Run(() =>
            floor.MoveDevice(
                request.PlacedFloorDeviceId,
                request.X,
                request.Y,
                request.FloorRoomId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
