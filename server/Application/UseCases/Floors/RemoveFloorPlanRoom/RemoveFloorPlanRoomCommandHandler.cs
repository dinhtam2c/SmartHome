using Application.BusinessServices.Floors;
using Application.Common.Errors;
using Application.Ports.Persistence;
using Domain.Models.Floors;
using MediatR;

namespace Application.UseCases.Floors.RemoveFloorPlanRoom;

public sealed class RemoveFloorPlanRoomCommandHandler
    : IRequestHandler<RemoveFloorPlanRoomCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly FloorPlanConsistencyService _consistencyService;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFloorPlanRoomCommandHandler(
        IFloorRepository floorRepository,
        FloorPlanConsistencyService consistencyService,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _consistencyService = consistencyService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        RemoveFloorPlanRoomCommand request,
        CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository, request.HomeId, request.FloorId, cancellationToken);
        var room = floor.FloorPlanRooms.FirstOrDefault(item => item.Id == request.FloorPlanRoomId)
            ?? throw new FloorPlanRoomNotFoundException(request.FloorPlanRoomId);

        await _consistencyService.RemoveRoomPlacements(
            floor, room.RoomId, cancellationToken);
        FloorCommandHelper.Run(() => floor.RemoveRoom(room.Id));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
