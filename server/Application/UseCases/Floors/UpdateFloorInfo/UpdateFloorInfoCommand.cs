using MediatR;

namespace Application.UseCases.Floors.UpdateFloorInfo;

public sealed record UpdateFloorInfoCommand(
    Guid HomeId,
    Guid FloorId,
    string? Name,
    int? CanvasWidth,
    int? CanvasHeight
) : IRequest;
