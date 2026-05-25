using Application.Common.Realtime;
using Application.Ports.Persistence;
using Application.Ports.Realtime;
using Domain.Models.Floors;
using MediatR;

namespace Application.UseCases.Floors.DeleteFloor;

public sealed class DeleteFloorCommandHandler : IRequestHandler<DeleteFloorCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFloorCommandHandler(
        IFloorRepository floorRepository,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteFloorCommand request, CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository,
            request.HomeId,
            request.FloorId,
            cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            _floorRepository.Remove(floor);

            await _unitOfWork.SaveChangesAsync(ct);

            var remainingFloors = await _floorRepository.ListByHomeId(request.HomeId, ct);
            for (var index = 0; index < remainingFloors.Count; index++)
            {
                remainingFloors[index].SetSortOrder(index + 1);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        await _realtimePublisher.PublishToHome(
            request.HomeId,
            RealtimeDelta.Create(
                entity: RealtimeEntities.Floor,
                change: RealtimeChanges.Deleted,
                homeId: request.HomeId,
                floorId: request.FloorId,
                delta: new
                {
                    Reason = FloorChangeReasons.Deleted,
                    ListChanged = true
                }),
            cancellationToken);
    }
}
