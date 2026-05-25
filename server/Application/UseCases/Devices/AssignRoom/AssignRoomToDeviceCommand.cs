using MediatR;

namespace Application.UseCases.Devices.AssignRoom;

public sealed record AssignRoomToDeviceCommand(
    Guid DeviceId,
    Guid? RoomId
) : IRequest;
