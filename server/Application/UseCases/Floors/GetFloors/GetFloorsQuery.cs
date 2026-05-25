using MediatR;

namespace Application.UseCases.Floors.GetFloors;

public sealed record GetFloorsQuery(Guid HomeId) : IRequest<IReadOnlyList<FloorSummaryDto>>;
