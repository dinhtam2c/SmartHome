using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Homes.Rooms.CreateRoom;

public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, Guid>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoomCommandHandler(
        IHomeRepository homeRepository,
        IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
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
