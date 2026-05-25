using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Homes.Rooms.UpdateRoom;

public class UpdateRoomCommandHandler : IRequestHandler<UpdateRoomCommand>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoomCommandHandler(
        IHomeRepository homeRepository,
        IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        var home = await _homeRepository.GetById(request.HomeId)
            ?? throw new HomeNotFoundException(request.HomeId);

        if (home.Rooms.All(room => room.Id != request.RoomId))
            throw new RoomNotFoundException(request.RoomId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Room name is required.");

        home.UpdateRoom(request.RoomId, request.Name, request.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
