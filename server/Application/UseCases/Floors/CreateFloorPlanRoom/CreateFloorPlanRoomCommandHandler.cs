using Application.Common.Errors;
using Application.Ports.Persistence;
using Domain.Models.Floors;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Floors.CreateFloorPlanRoom;

public sealed class CreateFloorPlanRoomCommandHandler
    : IRequestHandler<CreateFloorPlanRoomCommand, Guid>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFloorPlanRoomCommandHandler(
        IFloorRepository floorRepository,
        IHomeRepository homeRepository,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(
        CreateFloorPlanRoomCommand request,
        CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository, request.HomeId, request.FloorId, cancellationToken);
        var home = await _homeRepository.GetById(request.HomeId, cancellationToken)
            ?? throw new HomeNotFoundException(request.HomeId);

        if (home.Rooms.All(room => room.Id != request.RoomId))
            throw new RoomNotFoundException(request.RoomId);

        if (await _floorRepository.GetByRoomId(request.RoomId, cancellationToken) is not null)
            throw new ConflictException("Room is already represented on a floor plan.");

        var room = FloorCommandHelper.Run(() => floor.AddRoom(
            request.RoomId,
            FloorCommandHelper.ToDomainPoints(request.Polygon),
            request.FillColor));

        await FloorCommandHelper.SaveWithConflict(
            _unitOfWork,
            "Room is already represented on a floor plan.",
            cancellationToken);
        return room.Id;
    }
}
