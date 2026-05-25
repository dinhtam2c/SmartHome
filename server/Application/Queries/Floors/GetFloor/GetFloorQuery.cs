using MediatR;

namespace Application.Queries.Floors.GetFloor;

public sealed record GetFloorQuery(Guid HomeId, Guid FloorId) : IRequest<FloorDto>;
