using Application.BusinessServices.Floors;
using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Floors;
using MediatR;

namespace Application.UseCases.Floors.UpdateFloorPlanRoom;

public sealed class UpdateFloorPlanRoomCommandHandler
    : IRequestHandler<UpdateFloorPlanRoomCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly FloorPlanConsistencyService _consistencyService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFloorPlanRoomCommandHandler(
        IFloorRepository floorRepository,
        FloorPlanConsistencyService consistencyService,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _consistencyService = consistencyService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        UpdateFloorPlanRoomCommand request,
        CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository, request.HomeId, request.FloorId, cancellationToken);
        var room = floor.FloorPlanRooms.FirstOrDefault(item => item.Id == request.FloorPlanRoomId)
            ?? throw new FloorPlanRoomNotFoundException(request.FloorPlanRoomId);

        FloorCommandHelper.Run(() => floor.UpdateRoom(
            request.FloorPlanRoomId,
            FloorCommandHelper.ToDomainPoints(request.Polygon),
            request.FillColor));
        await _consistencyService.ReconcileRoomGeometry(
            floor, room.RoomId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
