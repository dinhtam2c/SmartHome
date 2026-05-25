using MediatR;

namespace Application.UseCases.Floors.CreateFloor;

public sealed record CreateFloorCommand(
    Guid HomeId,
    string Name,
    int CanvasWidth,
    int CanvasHeight
) : IRequest<Guid>;
