using MediatR;

namespace Application.UseCases.Floors.GetFloor;

public sealed record GetFloorQuery(Guid HomeId, Guid FloorId) : IRequest<FloorDto>;
