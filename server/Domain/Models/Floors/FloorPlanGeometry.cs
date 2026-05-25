namespace Domain.Models.Floors;

public static class FloorPlanGeometry
{
    private const double Epsilon = 0.0001;

    public static void ValidatePolygon(IReadOnlyCollection<FloorPoint> polygon)
    {
        if (polygon is null)
            throw new InvalidOperationException("Polygon is required.");

        if (polygon.Count < 3)
            throw new InvalidOperationException("A room polygon must have at least 3 points.");

        var points = polygon.ToList();
        if (points.Any(point => !float.IsFinite(point.X) || !float.IsFinite(point.Y)))
            throw new InvalidOperationException("Polygon points must be finite numbers.");

        if (points.Distinct().Count() != points.Count)
            throw new InvalidOperationException("Polygon cannot contain duplicate points.");

        for (var left = 0; left < points.Count; left++)
        {
            var leftNext = (left + 1) % points.Count;
            for (var right = left + 1; right < points.Count; right++)
            {
                var rightNext = (right + 1) % points.Count;
                if (left == right || leftNext == right || rightNext == left)
                    continue;

                if (SegmentsIntersect(points[left], points[leftNext], points[right], points[rightNext]))
                    throw new InvalidOperationException("Polygon cannot intersect itself.");
            }
        }

        if (Math.Abs(SignedArea(points)) <= Epsilon)
            throw new InvalidOperationException("Polygon must have a non-zero area.");
    }

    public static bool HasInteriorOverlap(
        IReadOnlyList<FloorPoint> left,
        IReadOnlyList<FloorPoint> right)
    {
        for (var leftIndex = 0; leftIndex < left.Count; leftIndex++)
        {
            var leftStart = left[leftIndex];
            var leftEnd = left[(leftIndex + 1) % left.Count];

            for (var rightIndex = 0; rightIndex < right.Count; rightIndex++)
            {
                var rightStart = right[rightIndex];
                var rightEnd = right[(rightIndex + 1) % right.Count];

                if (SegmentsCrossProperly(leftStart, leftEnd, rightStart, rightEnd))
                    return true;

                if (TryGetCollinearOverlap(
                        leftStart,
                        leftEnd,
                        rightStart,
                        rightEnd,
                        out var overlapStart,
                        out var overlapEnd)
                    && ShareInteriorBesideEdge(left, right, overlapStart, overlapEnd))
                    return true;
            }
        }

        return left.Any(point => ClassifyPoint(right, point) == PointLocation.Inside)
            || right.Any(point => ClassifyPoint(left, point) == PointLocation.Inside);
    }

    public static Guid? ResolveRoomId(
        IEnumerable<FloorPlanRoom> rooms,
        FloorPoint point)
    {
        var candidates = rooms
            .Where(room => ClassifyPoint(room.Polygon, point) != PointLocation.Outside)
            .Select(room => new
            {
                room.RoomId,
                Distance = SquaredDistance(point, GetCentroid(room.Polygon))
            })
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.RoomId)
            .ToList();

        return candidates.Count == 0 ? null : candidates[0].RoomId;
    }

    public static bool ContainsOrTouches(IReadOnlyList<FloorPoint> polygon, FloorPoint point)
    {
        return ClassifyPoint(polygon, point) != PointLocation.Outside;
    }

    private static PointLocation ClassifyPoint(IReadOnlyList<FloorPoint> polygon, FloorPoint point)
    {
        var inside = false;
        for (var current = 0; current < polygon.Count; current++)
        {
            var previous = (current + polygon.Count - 1) % polygon.Count;
            var start = polygon[previous];
            var end = polygon[current];

            if (PointOnSegment(point, start, end))
                return PointLocation.Boundary;

            var crosses = (start.Y > point.Y) != (end.Y > point.Y)
                && point.X < (end.X - start.X) * (point.Y - start.Y) / (end.Y - start.Y) + start.X;
            if (crosses)
                inside = !inside;
        }

        return inside ? PointLocation.Inside : PointLocation.Outside;
    }

    private static bool ShareInteriorBesideEdge(
        IReadOnlyList<FloorPoint> left,
        IReadOnlyList<FloorPoint> right,
        FloorPoint edgeStart,
        FloorPoint edgeEnd)
    {
        var midpoint = new FloorPoint(
            (edgeStart.X + edgeEnd.X) / 2,
            (edgeStart.Y + edgeEnd.Y) / 2);
        var dx = edgeEnd.X - edgeStart.X;
        var dy = edgeEnd.Y - edgeStart.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= Epsilon)
            return false;

        var offset = Math.Max(Epsilon * 10, length * 0.00001);
        var normalX = (float)(-dy / length * offset);
        var normalY = (float)(dx / length * offset);
        var first = new FloorPoint(midpoint.X + normalX, midpoint.Y + normalY);
        var second = new FloorPoint(midpoint.X - normalX, midpoint.Y - normalY);

        return ClassifyPoint(left, first) == PointLocation.Inside
                && ClassifyPoint(right, first) == PointLocation.Inside
            || ClassifyPoint(left, second) == PointLocation.Inside
                && ClassifyPoint(right, second) == PointLocation.Inside;
    }

    private static FloorPoint GetCentroid(IReadOnlyList<FloorPoint> polygon)
    {
        double weightedX = 0;
        double weightedY = 0;
        double areaFactorSum = 0;

        for (var index = 0; index < polygon.Count; index++)
        {
            var current = polygon[index];
            var next = polygon[(index + 1) % polygon.Count];
            var factor = current.X * next.Y - next.X * current.Y;
            areaFactorSum += factor;
            weightedX += (current.X + next.X) * factor;
            weightedY += (current.Y + next.Y) * factor;
        }

        var denominator = 3 * areaFactorSum;
        return new FloorPoint((float)(weightedX / denominator), (float)(weightedY / denominator));
    }

    private static double SignedArea(IReadOnlyList<FloorPoint> polygon)
    {
        double sum = 0;
        for (var index = 0; index < polygon.Count; index++)
        {
            var current = polygon[index];
            var next = polygon[(index + 1) % polygon.Count];
            sum += current.X * next.Y - next.X * current.Y;
        }

        return sum / 2;
    }

    private static double SquaredDistance(FloorPoint left, FloorPoint right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return dx * dx + dy * dy;
    }

    private static bool SegmentsIntersect(FloorPoint a, FloorPoint b, FloorPoint c, FloorPoint d)
    {
        var abC = Cross(a, b, c);
        var abD = Cross(a, b, d);
        var cdA = Cross(c, d, a);
        var cdB = Cross(c, d, b);

        if (HaveOppositeSigns(abC, abD) && HaveOppositeSigns(cdA, cdB))
            return true;

        return Math.Abs(abC) <= Epsilon && PointOnSegment(c, a, b)
            || Math.Abs(abD) <= Epsilon && PointOnSegment(d, a, b)
            || Math.Abs(cdA) <= Epsilon && PointOnSegment(a, c, d)
            || Math.Abs(cdB) <= Epsilon && PointOnSegment(b, c, d);
    }

    private static bool SegmentsCrossProperly(FloorPoint a, FloorPoint b, FloorPoint c, FloorPoint d)
    {
        return HaveOppositeSigns(Cross(a, b, c), Cross(a, b, d))
            && HaveOppositeSigns(Cross(c, d, a), Cross(c, d, b));
    }

    private static bool TryGetCollinearOverlap(
        FloorPoint a,
        FloorPoint b,
        FloorPoint c,
        FloorPoint d,
        out FloorPoint overlapStart,
        out FloorPoint overlapEnd)
    {
        overlapStart = default;
        overlapEnd = default;
        if (Math.Abs(Cross(a, b, c)) > Epsilon || Math.Abs(Cross(a, b, d)) > Epsilon)
            return false;

        var sharedPoints = new[] { a, b, c, d }
            .Where(point => PointOnSegment(point, a, b) && PointOnSegment(point, c, d))
            .Distinct()
            .ToList();
        if (sharedPoints.Count < 2)
            return false;

        var useX = Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y);
        var ordered = sharedPoints
            .OrderBy(point => useX ? point.X : point.Y)
            .ToList();
        overlapStart = ordered[0];
        overlapEnd = ordered[^1];
        return SquaredDistance(overlapStart, overlapEnd) > Epsilon * Epsilon;
    }

    private static bool PointOnSegment(FloorPoint point, FloorPoint start, FloorPoint end)
    {
        if (Math.Abs(Cross(start, end, point)) > Epsilon)
            return false;

        return point.X >= Math.Min(start.X, end.X) - Epsilon
            && point.X <= Math.Max(start.X, end.X) + Epsilon
            && point.Y >= Math.Min(start.Y, end.Y) - Epsilon
            && point.Y <= Math.Max(start.Y, end.Y) + Epsilon;
    }

    private static double Cross(FloorPoint start, FloorPoint end, FloorPoint point)
    {
        return (end.X - start.X) * (point.Y - start.Y)
            - (end.Y - start.Y) * (point.X - start.X);
    }

    private static bool HaveOppositeSigns(double left, double right)
    {
        return left > Epsilon && right < -Epsilon || left < -Epsilon && right > Epsilon;
    }

    private enum PointLocation
    {
        Outside,
        Boundary,
        Inside
    }
}
