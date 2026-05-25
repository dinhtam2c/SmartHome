using MediatR;

namespace Application.UseCases.Floors.ReorderFloors;

public sealed record ReorderFloorsCommand(Guid HomeId, IReadOnlyList<Guid> FloorIds) : IRequest;
