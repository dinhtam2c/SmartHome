using Application.Ports.Persistence;
using Domain.Models.Floors;
using MediatR;

namespace Application.UseCases.Floors.UpdateFloorInfo;

public sealed class UpdateFloorInfoCommandHandler : IRequestHandler<UpdateFloorInfoCommand>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFloorInfoCommandHandler(
        IFloorRepository floorRepository,
        IUnitOfWork unitOfWork)
    {
        _floorRepository = floorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateFloorInfoCommand request, CancellationToken cancellationToken)
    {
        var floor = await FloorCommandHelper.GetFloorForHome(
            _floorRepository,
            request.HomeId,
            request.FloorId,
            cancellationToken);

        FloorCommandHelper.Run(() =>
            floor.UpdateInfo(request.Name, request.CanvasWidth, request.CanvasHeight));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
