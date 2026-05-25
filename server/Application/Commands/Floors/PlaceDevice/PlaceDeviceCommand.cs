using MediatR;

namespace Application.Commands.Floors.PlaceDevice;

public sealed record PlaceDeviceCommand(
    Guid HomeId,
    Guid FloorId,
    Guid DeviceId,
    float X,
    float Y,
    Guid? FloorRoomId
) : IRequest<Guid>;
