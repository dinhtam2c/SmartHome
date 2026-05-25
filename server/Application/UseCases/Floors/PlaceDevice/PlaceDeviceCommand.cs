using MediatR;

namespace Application.UseCases.Floors.PlaceDevice;

public sealed record PlaceDeviceCommand(
    Guid HomeId,
    Guid FloorId,
    Guid DeviceId,
    float X,
    float Y
) : IRequest<Guid>;
