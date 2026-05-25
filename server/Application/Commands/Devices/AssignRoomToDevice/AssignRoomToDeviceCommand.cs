using MediatR;

namespace Application.Commands.Devices.AssignRoomToDevice;

public sealed record AssignRoomToDeviceCommand(
    Guid DeviceId,
    Guid RoomId
) : IRequest;
