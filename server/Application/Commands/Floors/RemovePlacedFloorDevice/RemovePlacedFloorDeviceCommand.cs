using MediatR;

namespace Application.Commands.Floors.RemovePlacedFloorDevice;

public sealed record RemovePlacedFloorDeviceCommand(
    Guid HomeId,
    Guid FloorId,
    Guid PlacedFloorDeviceId
) : IRequest;
