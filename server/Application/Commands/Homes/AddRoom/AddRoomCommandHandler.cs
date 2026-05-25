using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.AddRoom;

public class AddRoomCommandHandler : IRequestHandler<AddRoomCommand, Guid>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddRoomCommandHandler(
        IHomeRepository homeRepository,
        IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddRoomCommand request, CancellationToken cancellationToken)
    {
        var home = await _homeRepository.GetById(request.HomeId)
            ?? throw new HomeNotFoundException(request.HomeId);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Room name is required.");

        var room = home.AddRoom(request.Name, request.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return room.Id;
    }
}
