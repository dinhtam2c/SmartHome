using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Homes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Commands.Homes.DeleteRoom;

public class DeleteRoomCommandHandler : IRequestHandler<DeleteRoomCommand>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IAppReadDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoomCommandHandler(
        IHomeRepository homeRepository,
        IAppReadDbContext context,
        IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        var home = await _homeRepository.GetById(request.HomeId)
            ?? throw new HomeNotFoundException(request.HomeId);

        if (home.Rooms.All(room => room.Id != request.RoomId))
            throw new RoomNotFoundException(request.RoomId);

        var hasDevices = await _context.Devices
            .AsNoTracking()
            .AnyAsync(
                device => device.HomeId == request.HomeId && device.RoomId == request.RoomId,
                cancellationToken);

        if (hasDevices)
            throw new DomainValidationException("Room has assigned devices and cannot be deleted.");

        home.RemoveRoom(request.RoomId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
