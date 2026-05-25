using MediatR;

namespace Application.Commands.Floors.ReorderFloors;

public sealed record ReorderFloorsCommand(Guid HomeId, IReadOnlyList<Guid> FloorIds) : IRequest;
