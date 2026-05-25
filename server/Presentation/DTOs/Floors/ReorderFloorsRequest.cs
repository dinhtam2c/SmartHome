namespace Presentation.Floors;

public sealed record ReorderFloorsRequest(IReadOnlyList<Guid> FloorIds);
