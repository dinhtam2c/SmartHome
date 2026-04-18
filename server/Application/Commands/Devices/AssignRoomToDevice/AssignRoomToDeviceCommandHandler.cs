using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Devices;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Devices.AssignRoomToDevice;

public sealed class AssignRoomToDeviceCommandHandler : IRequestHandler<AssignRoomToDeviceCommand>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignRoomToDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        IHomeRepository homeRepository,
        IUnitOfWork unitOfWork
    )
    {
        _deviceRepository = deviceRepository;
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignRoomToDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetById(request.DeviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        if (!device.HomeId.HasValue)
            throw new DomainValidationException("Device is not assigned to a home.");

        var home = await _homeRepository.GetById(device.HomeId.Value, cancellationToken)
            ?? throw new HomeNotFoundException(device.HomeId.Value);

        var room = home.Rooms.FirstOrDefault(l => l.Id == request.RoomId)
            ?? throw new RoomNotFoundException(request.RoomId);

        try
        {
            device.AssignRoom(room.Id);
        }
        catch (InvalidOperationException ex)
        {
            throw new DomainValidationException(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
