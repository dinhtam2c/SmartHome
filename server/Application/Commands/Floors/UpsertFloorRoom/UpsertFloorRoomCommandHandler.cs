using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Floors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Commands.Floors.UpsertFloorRoom;

public sealed class UpsertFloorRoomCommandHandler
    : IRequestHandler<UpsertFloorRoomCommand, Guid>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IAppReadDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertFloorRoomCommandHandler(
        IFloorRepository floorRepository,
        IAppReadDbContext context,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(UpsertFloorRoomCommand request, CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository,
            request.HomeId,
            request.FloorId,
            cancellationToken);

        if (request.LinkedRoomId.HasValue)
        {
            var roomExists = await _context.Homes
                .AsNoTracking()
                .Where(home => home.Id == request.HomeId)
                .SelectMany(home => home.Rooms)
                .AnyAsync(room => room.Id == request.LinkedRoomId.Value, cancellationToken);

            if (!roomExists)
                throw new RoomNotFoundException(request.LinkedRoomId.Value);
        }

        var polygon = FloorCommandHelper.ToDomainPoints(request.Polygon);

        if (request.RoomId.HasValue)
        {
            FloorCommandHelper.Run(() =>
                floor.UpdateRoom(
                    request.RoomId.Value,
                    request.LinkedRoomId,
                    request.Label,
                    polygon,
                    request.FillColor));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return request.RoomId.Value;
        }

        var room = FloorCommandHelper.Run(() =>
            floor.AddRoom(
                request.LinkedRoomId,
                request.Label,
                polygon,
                request.FillColor));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return room.Id;
    }
}
