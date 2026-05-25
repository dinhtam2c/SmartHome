using Application.Common.Data;
using Application.Common.Realtime;
using Application.Exceptions;
using Core.Domain.Floors;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Floors.ReorderFloors;

public sealed class ReorderFloorsCommandHandler : IRequestHandler<ReorderFloorsCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderFloorsCommandHandler(
        IFloorRepository floorRepository,
        IHomeRepository homeRepository,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _homeRepository = homeRepository;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReorderFloorsCommand request, CancellationToken cancellationToken)
    {
        _ = await _homeRepository.GetById(request.HomeId, cancellationToken)
            ?? throw new HomeNotFoundException(request.HomeId);

        var floors = await _floorRepository.ListByHomeId(request.HomeId, cancellationToken);
        var existingIds = floors.Select(floor => floor.Id).ToHashSet();
        var requestedIds = request.FloorIds.ToList();

        if (requestedIds.Count != floors.Count
            || requestedIds.Distinct().Count() != requestedIds.Count
            || requestedIds.Any(floorId => !existingIds.Contains(floorId)))
        {
            throw new DomainValidationException("Floor order must include every floor exactly once.");
        }

        var floorMap = floors.ToDictionary(floor => floor.Id);
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            for (var index = 0; index < requestedIds.Count; index++)
            {
                floorMap[requestedIds[index]].SetSortOrder(requestedIds.Count + index + 1);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            for (var index = 0; index < requestedIds.Count; index++)
            {
                floorMap[requestedIds[index]].SetSortOrder(index + 1);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        await _realtimePublisher.PublishToHome(
            request.HomeId,
            RealtimeDelta.Create(
                entity: RealtimeEntities.Floor,
                change: RealtimeChanges.Updated,
                homeId: request.HomeId,
                delta: new
                {
                    Reason = "Reordered",
                    ListChanged = true
                }),
            cancellationToken);
    }
}
