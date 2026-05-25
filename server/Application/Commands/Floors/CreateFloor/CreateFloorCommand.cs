using MediatR;

namespace Application.Commands.Floors.CreateFloor;

public sealed record CreateFloorCommand(
    Guid HomeId,
    string Name,
    int CanvasWidth,
    int CanvasHeight
) : IRequest<Guid>;
