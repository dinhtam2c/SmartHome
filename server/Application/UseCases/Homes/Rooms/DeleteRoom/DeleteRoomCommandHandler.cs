using Application.Common.Errors;
using Application.Ports.Persistence;
using Domain.Models.Devices;
using Domain.Models.Floors;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Homes.Rooms.DeleteRoom;

public sealed class DeleteRoomCommandHandler : IRequestHandler<DeleteRoomCommand>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IFloorRepository _floorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoomCommandHandler(
        IHomeRepository homeRepository,
        IDeviceRepository deviceRepository,
        IFloorRepository floorRepository,
        IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _deviceRepository = deviceRepository;
        _floorRepository = floorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        var home = await _homeRepository.GetById(request.HomeId, cancellationToken)
            ?? throw new HomeNotFoundException(request.HomeId);
        if (home.Rooms.All(room => room.Id != request.RoomId))
            throw new RoomNotFoundException(request.RoomId);

        var devices = await _deviceRepository.ListByRoomId(request.RoomId, cancellationToken);
        foreach (var device in devices)
            device.AssignRoom(null);

        var floor = await _floorRepository.GetByRoomId(request.RoomId, cancellationToken);
        if (floor is not null)
        {
            floor.RemoveDevicePlacements(devices.Select(device => device.Id));
            var floorPlanRoom = floor.FloorPlanRooms.Single(room => room.RoomId == request.RoomId);
            floor.RemoveRoom(floorPlanRoom.Id);
        }

        home.RemoveRoom(request.RoomId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
