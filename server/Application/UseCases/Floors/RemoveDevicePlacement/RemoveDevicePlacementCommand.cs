using MediatR;

namespace Application.UseCases.Floors.RemoveDevicePlacement;

public sealed record RemoveDevicePlacementCommand(
    Guid HomeId,
    Guid FloorId,
    Guid PlacementId
) : IRequest;
