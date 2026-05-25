using Application.Ports.Persistence;
using Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Floors.GetFloors;

public sealed class GetFloorsQueryHandler
    : IRequestHandler<GetFloorsQuery, IReadOnlyList<FloorSummaryDto>>
{
    private readonly IAppReadDbContext _context;

    public GetFloorsQueryHandler(IAppReadDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FloorSummaryDto>> Handle(
        GetFloorsQuery request,
        CancellationToken cancellationToken)
    {
        var homeExists = await _context.Homes
            .AsNoTracking()
            .AnyAsync(home => home.Id == request.HomeId, cancellationToken);

        if (!homeExists)
            throw new HomeNotFoundException(request.HomeId);

        var floors = await _context.Floors
            .AsNoTracking()
            .Include(floor => floor.FloorPlanRooms)
            .Include(floor => floor.DevicePlacements)
            .AsSplitQuery()
            .Where(floor => floor.HomeId == request.HomeId)
            .OrderBy(floor => floor.SortOrder)
            .ToListAsync(cancellationToken);

        return floors
            .Select(floor => new FloorSummaryDto(
                floor.Id,
                floor.HomeId,
                floor.Name,
                floor.SortOrder,
                floor.CanvasWidth,
                floor.CanvasHeight,
                floor.CreatedAt,
                floor.UpdatedAt,
                floor.FloorPlanRooms.Count,
                floor.DevicePlacements.Count,
                floor.FloorPlanRooms
                    .Select(room => room.RoomId)
                    .OrderBy(roomId => roomId)
                    .ToList(),
                floor.DevicePlacements
                    .Select(placement => placement.DeviceId)
                    .OrderBy(deviceId => deviceId)
                    .ToList()))
            .ToList();
    }
}
