using Application.Common.Errors;
using Application.Ports.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Floors.GetFloor;

public sealed class GetFloorQueryHandler : IRequestHandler<GetFloorQuery, FloorDto>
{
    private readonly IAppReadDbContext _context;

    public GetFloorQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<FloorDto> Handle(GetFloorQuery request, CancellationToken cancellationToken)
    {
        var homeExists = await _context.Homes
            .AsNoTracking()
            .AnyAsync(home => home.Id == request.HomeId, cancellationToken);
        if (!homeExists)
            throw new HomeNotFoundException(request.HomeId);

        var floor = await _context.Floors
            .AsNoTracking()
            .Include(item => item.FloorPlanRooms)
            .Include(item => item.DevicePlacements)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                item => item.HomeId == request.HomeId && item.Id == request.FloorId,
                cancellationToken)
            ?? throw new FloorNotFoundException(request.FloorId);

        return new FloorDto(
            floor.Id,
            floor.HomeId,
            floor.Name,
            floor.SortOrder,
            floor.CanvasWidth,
            floor.CanvasHeight,
            floor.CreatedAt,
            floor.UpdatedAt,
            floor.FloorPlanRooms
                .OrderBy(room => room.RoomId)
                .Select(room => new FloorPlanRoomDto(
                    room.Id,
                    room.RoomId,
                    room.Polygon.Select(point => new FloorPointDto(point.X, point.Y)).ToList(),
                    room.FillColor))
                .ToList(),
            floor.DevicePlacements
                .OrderBy(placement => placement.DeviceId)
                .Select(placement => new FloorDevicePlacementDto(
                    placement.Id,
                    placement.DeviceId,
                    placement.X,
                    placement.Y))
                .ToList());
    }
}
