using MediatR;

namespace Application.Commands.Floors.MoveDevice;

public sealed record MoveDeviceCommand(
    Guid HomeId,
    Guid FloorId,
    Guid PlacedFloorDeviceId,
    float X,
    float Y,
    Guid? FloorRoomId
) : IRequest;
