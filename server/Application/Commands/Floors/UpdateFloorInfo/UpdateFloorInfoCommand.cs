using MediatR;

namespace Application.Commands.Floors.UpdateFloorInfo;

public sealed record UpdateFloorInfoCommand(
    Guid HomeId,
    Guid FloorId,
    string? Name,
    int? CanvasWidth,
    int? CanvasHeight
) : IRequest;
