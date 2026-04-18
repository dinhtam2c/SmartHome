using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Devices;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Devices.AddDevice;

public class AddDeviceCommandHandler : IRequestHandler<AddDeviceCommand, Guid>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        IHomeRepository homeRepository,
        IUnitOfWork unitOfWork
    )
    {
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
        _homeRepository = homeRepository;
    }

    public async Task<Guid> Handle(AddDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByProvisionCode(request.ProvisionCode)
            ?? throw new InvalidProvisionCodeException(request.ProvisionCode);

        var home = await _homeRepository.GetById(request.HomeId)
            ?? throw new HomeNotFoundException(request.HomeId);

        var room = home.Rooms.FirstOrDefault(l => l.Id == request.RoomId);

        device.ConfirmProvisioning(home.Id, room?.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return device.Id;
    }
}
