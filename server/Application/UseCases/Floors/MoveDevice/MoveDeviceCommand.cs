using MediatR;

namespace Application.UseCases.Floors.MoveDevice;

public sealed record MoveDeviceCommand(
    Guid HomeId,
    Guid FloorId,
    Guid PlacementId,
    float X,
    float Y
) : IRequest;
