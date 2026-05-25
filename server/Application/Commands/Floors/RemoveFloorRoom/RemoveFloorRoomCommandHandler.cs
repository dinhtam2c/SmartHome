using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Floors;
using MediatR;

namespace Application.Commands.Floors.RemoveFloorRoom;

public sealed class RemoveFloorRoomCommandHandler : IRequestHandler<RemoveFloorRoomCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFloorRoomCommandHandler(
        IFloorRepository floorRepository,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveFloorRoomCommand request, CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository,
            request.HomeId,
            request.FloorId,
            cancellationToken);

        FloorCommandHelper.Run(() => floor.RemoveRoom(request.RoomId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
