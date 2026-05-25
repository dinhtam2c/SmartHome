using Application.Ports.Persistence;
using Application.Ports.Messages;
using Application.Common.Errors;
using Domain.Models.Devices;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Devices.ClaimDevice;

public class ClaimDeviceCommandHandler : IRequestHandler<ClaimDeviceCommand, Guid>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeviceAccessManager _deviceAccessManager;

    public ClaimDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        IHomeRepository homeRepository,
        IDeviceAccessManager deviceAccessManager,
        IUnitOfWork unitOfWork
    )
    {
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
        _homeRepository = homeRepository;
        _deviceAccessManager = deviceAccessManager;
    }

    public async Task<Guid> Handle(ClaimDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByProvisionCode(request.ProvisionCode)
            ?? throw new InvalidProvisionCodeException(request.ProvisionCode);

        var home = await _homeRepository.GetById(request.HomeId)
            ?? throw new HomeNotFoundException(request.HomeId);

        var room = request.RoomId.HasValue
            ? home.Rooms.FirstOrDefault(l => l.Id == request.RoomId.Value)
                ?? throw new RoomNotFoundException(request.RoomId.Value)
            : null;

        device.ConfirmProvisioning(home.Id, room?.Id);

        await _deviceAccessManager.UpsertDeviceAccess(
            device.Id,
            device.AccessToken,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return device.Id;
    }
}
