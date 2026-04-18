using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.DeleteRoom;

public class DeleteRoomCommandHandler : IRequestHandler<DeleteRoomCommand>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoomCommandHandler(
        IHomeRepository homeRepository,
        IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        var home = await _homeRepository.GetById(request.HomeId)
            ?? throw new HomeNotFoundException(request.HomeId);

        home.RemoveRoom(request.RoomId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
