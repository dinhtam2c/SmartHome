namespace WebAPI.Floors;

public sealed record ReorderFloorsRequest(IReadOnlyList<Guid> FloorIds);
