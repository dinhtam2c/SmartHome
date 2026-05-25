using Application.Common.Errors;
using Domain.Models.Floors;
using Application.Ports.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Floors;

internal static class FloorCommandHelper
{
    public static async Task<Floor> GetFloorForHome(
        IFloorRepository floorRepository,
        Guid homeId,
        Guid floorId,
        CancellationToken cancellationToken)
    {
        var floor = await floorRepository.GetById(floorId, cancellationToken)
            ?? throw new FloorNotFoundException(floorId);

        if (floor.HomeId != homeId)
            throw new FloorNotFoundException(floorId);

        return floor;
    }

    public static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException ex)
        {
            throw ToDomainValidationException(ex);
        }
        catch (ArgumentException ex)
        {
            throw ToDomainValidationException(ex);
        }
    }

    public static T Run<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (InvalidOperationException ex)
        {
            throw ToDomainValidationException(ex);
        }
        catch (ArgumentException ex)
        {
            throw ToDomainValidationException(ex);
        }
    }

    public static IReadOnlyList<FloorPoint> ToDomainPoints(
        IReadOnlyCollection<FloorPointModel>? points)
    {
        if (points is null)
            throw new DomainValidationException("Polygon is required.");

        return points
            .Select(point => new FloorPoint(point.X, point.Y))
            .ToList();
    }

    public static async Task SaveWithConflict(
        IUnitOfWork unitOfWork,
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(conflictMessage);
        }
    }

    private static DomainValidationException ToDomainValidationException(Exception ex)
    {
        return new DomainValidationException(ex.Message);
    }
}
