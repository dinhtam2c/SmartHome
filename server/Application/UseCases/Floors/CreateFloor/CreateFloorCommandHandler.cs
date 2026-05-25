using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Floors;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Floors.CreateFloor;

public sealed class CreateFloorCommandHandler : IRequestHandler<CreateFloorCommand, Guid>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFloorCommandHandler(
        IFloorRepository floorRepository,
        IHomeRepository homeRepository,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateFloorCommand request, CancellationToken cancellationToken)
    {
        _ = await _homeRepository.GetById(request.HomeId, cancellationToken)
            ?? throw new HomeNotFoundException(request.HomeId);

        var sortOrder = await _floorRepository.GetNextSortOrder(request.HomeId, cancellationToken);

        var floor = FloorCommandHelper.Run(() =>
            Floor.Create(request.HomeId, request.Name, request.CanvasWidth, request.CanvasHeight, sortOrder));

        await _floorRepository.Add(floor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return floor.Id;
    }
}
