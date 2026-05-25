using Application.BusinessServices.Floors;
using Application.Common.Errors;
using Application.Ports.Persistence;
using Domain.Models.Devices;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Devices.AssignRoom;

public sealed class AssignRoomToDeviceCommandHandler : IRequestHandler<AssignRoomToDeviceCommand>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FloorPlanConsistencyService _floorPlanConsistency;

    public AssignRoomToDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        IHomeRepository homeRepository,
        FloorPlanConsistencyService floorPlanConsistency,
        IUnitOfWork unitOfWork
    )
    {
        _deviceRepository = deviceRepository;
        _homeRepository = homeRepository;
        _floorPlanConsistency = floorPlanConsistency;
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

        if (request.RoomId.HasValue
            && home.Rooms.All(room => room.Id != request.RoomId.Value))
            throw new RoomNotFoundException(request.RoomId.Value);

        try
        {
            await _floorPlanConsistency.ReconcileDeviceRoom(
                device, request.RoomId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new DomainValidationException(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
