using System.Text.RegularExpressions;

namespace Domain.Models.Floors;

public class FloorPlanRoom
{
    private static readonly Regex FillColorRegex =
        new("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$");

    public Guid Id { get; private set; }
    public Guid FloorId { get; private set; }
    public Guid RoomId { get; private set; }
    public IReadOnlyList<FloorPoint> Polygon { get; private set; } = [];
    public string? FillColor { get; private set; }

    private FloorPlanRoom()
    {
    }

    internal FloorPlanRoom(
        Guid floorId,
        Guid roomId,
        IReadOnlyCollection<FloorPoint> polygon,
        string? fillColor)
    {
        if (floorId == Guid.Empty)
            throw new InvalidOperationException("FloorId is required.");

        if (roomId == Guid.Empty)
            throw new InvalidOperationException("RoomId is required.");

        Id = Guid.NewGuid();
        FloorId = floorId;
        RoomId = roomId;
        ReplaceVisuals(polygon, fillColor);
    }

    internal void ReplaceVisuals(
        IReadOnlyCollection<FloorPoint> polygon,
        string? fillColor)
    {
        FloorPlanGeometry.ValidatePolygon(polygon);
        Polygon = polygon.ToList();
        FillColor = NormalizeFillColor(fillColor);
    }

    private static string? NormalizeFillColor(string? fillColor)
    {
        if (string.IsNullOrWhiteSpace(fillColor))
            return null;

        var normalized = fillColor.Trim();
        if (!FillColorRegex.IsMatch(normalized))
            throw new InvalidOperationException("FillColor must be a valid hex color.");

        return normalized.ToUpperInvariant();
    }
}
