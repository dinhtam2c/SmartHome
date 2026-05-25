using Application.Common.Data;
using Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Queries.Floors.GetFloors;

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
            .Include(floor => floor.Rooms)
            .Include(floor => floor.PlacedFloorDevices)
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
                floor.Rooms.Count,
                floor.PlacedFloorDevices.Count,
                floor.PlacedFloorDevices
                    .Select(placedFloorDevice => placedFloorDevice.DeviceId)
                    .OrderBy(deviceId => deviceId)
                    .ToList()))
            .ToList();
    }
}
