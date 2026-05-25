using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Floors;
using MediatR;

namespace Application.UseCases.Floors.RemoveDevicePlacement;

public sealed class RemoveDevicePlacementCommandHandler
    : IRequestHandler<RemoveDevicePlacementCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveDevicePlacementCommandHandler(
        IFloorRepository floorRepository,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        RemoveDevicePlacementCommand request,
        CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository, request.HomeId, request.FloorId, cancellationToken);
        if (floor.DevicePlacements.All(placement => placement.Id != request.PlacementId))
            throw new FloorDevicePlacementNotFoundException(request.PlacementId);

        FloorCommandHelper.Run(() => floor.RemoveDevicePlacement(request.PlacementId));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
