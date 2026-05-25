using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.Domain.Floors;

public class FloorRoom
{
    private static readonly Regex FillColorRegex =
        new("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$");

    public Guid Id { get; private set; }
    public Guid FloorId { get; private set; }
    public Guid? LinkedRoomId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string PolygonPayload { get; private set; } = "[]";
    public string? FillColor { get; private set; }

    private FloorRoom()
    {
    }

    internal FloorRoom(
        Guid floorId,
        Guid? linkedRoomId,
        string label,
        IReadOnlyCollection<FloorPoint> polygon,
        string? fillColor)
    {
        if (floorId == Guid.Empty)
            throw new InvalidOperationException("FloorId is required.");

        Id = Guid.NewGuid();
        FloorId = floorId;

        Replace(linkedRoomId, label, polygon, fillColor);
    }

    internal void Replace(
        Guid? linkedRoomId,
        string label,
        IReadOnlyCollection<FloorPoint> polygon,
        string? fillColor)
    {
        LinkedRoomId = NormalizeLinkedRoomId(linkedRoomId);
        Label = NormalizeLabel(label);
        PolygonPayload = JsonSerializer.Serialize(NormalizePolygon(polygon));
        FillColor = NormalizeFillColor(fillColor);
    }

    public IReadOnlyList<FloorPoint> GetPolygon()
    {
        try
        {
            return JsonSerializer.Deserialize<List<FloorPoint>>(PolygonPayload) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Room polygon payload is invalid.", ex);
        }
    }

    private static Guid? NormalizeLinkedRoomId(Guid? linkedRoomId)
    {
        if (!linkedRoomId.HasValue)
            return null;

        if (linkedRoomId.Value == Guid.Empty)
            throw new InvalidOperationException("LinkedRoomId must not be empty.");

        return linkedRoomId;
    }

    private static string NormalizeLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new InvalidOperationException("Room label is required.");

        return label.Trim();
    }

    private static List<FloorPoint> NormalizePolygon(IReadOnlyCollection<FloorPoint> polygon)
    {
        if (polygon is null)
            throw new InvalidOperationException("Polygon is required.");

        if (polygon.Count < 3)
            throw new InvalidOperationException("A room polygon must have at least 3 points.");

        var points = polygon.ToList();
        if (points.Any(point => !float.IsFinite(point.X) || !float.IsFinite(point.Y)))
            throw new InvalidOperationException("Polygon points must be finite numbers.");

        return points;
    }

    private static string? NormalizeFillColor(string? fillColor)
    {
        if (fillColor is null)
            return null;

        var normalized = fillColor.Trim();
        if (normalized.Length == 0)
            return null;

        if (!FillColorRegex.IsMatch(normalized))
            throw new InvalidOperationException("FillColor must be a valid hex color.");

        return normalized.ToUpperInvariant();
    }
}
