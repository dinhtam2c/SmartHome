namespace WebAPI.Floors;

public sealed record UpdateFloorInfoRequest(string? Name, int? CanvasWidth, int? CanvasHeight);
