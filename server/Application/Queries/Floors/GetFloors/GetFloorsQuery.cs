using MediatR;

namespace Application.Queries.Floors.GetFloors;

public sealed record GetFloorsQuery(Guid HomeId) : IRequest<IReadOnlyList<FloorSummaryDto>>;
