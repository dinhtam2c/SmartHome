import type { Point } from "../types/floorTypes";

// Keep this tolerance and shared-boundary behavior aligned with backend FloorPlanGeometry.
const EPSILON = 0.0001;

type PointLocation = "outside" | "boundary" | "inside";

export function isPointInsidePolygon(point: Point, polygon: readonly Point[]) {
  return classifyPoint(point, polygon) === "inside";
}

export function segmentEntersPolygon(
  start: Point,
  end: Point,
  polygon: readonly Point[]
) {
  if (
    classifyPoint(start, polygon) === "inside" ||
    classifyPoint(end, polygon) === "inside"
  ) {
    return true;
  }

  for (let index = 0; index < polygon.length; index += 1) {
    const edgeStart = polygon[index];
    const edgeEnd = polygon[(index + 1) % polygon.length];

    if (segmentsCrossProperly(start, end, edgeStart, edgeEnd)) {
      return true;
    }
  }

  const midpoint = {
    x: (start.x + end.x) / 2,
    y: (start.y + end.y) / 2,
  };
  return classifyPoint(midpoint, polygon) === "inside";
}

export function hasInteriorOverlap(
  left: readonly Point[],
  right: readonly Point[]
) {
  for (let leftIndex = 0; leftIndex < left.length; leftIndex += 1) {
    const leftStart = left[leftIndex];
    const leftEnd = left[(leftIndex + 1) % left.length];

    for (let rightIndex = 0; rightIndex < right.length; rightIndex += 1) {
      const rightStart = right[rightIndex];
      const rightEnd = right[(rightIndex + 1) % right.length];

      if (segmentsCrossProperly(leftStart, leftEnd, rightStart, rightEnd)) {
        return true;
      }

      const collinearOverlap = getCollinearOverlap(
        leftStart,
        leftEnd,
        rightStart,
        rightEnd
      );
      if (
        collinearOverlap &&
        shareInteriorBesideEdge(
          left,
          right,
          collinearOverlap[0],
          collinearOverlap[1]
        )
      ) {
        return true;
      }
    }
  }

  return (
    left.some((point) => classifyPoint(point, right) === "inside") ||
    right.some((point) => classifyPoint(point, left) === "inside")
  );
}

function classifyPoint(point: Point, polygon: readonly Point[]): PointLocation {
  let inside = false;

  for (let current = 0; current < polygon.length; current += 1) {
    const previous = (current + polygon.length - 1) % polygon.length;
    const start = polygon[previous];
    const end = polygon[current];

    if (isPointOnSegment(point, start, end)) {
      return "boundary";
    }

    const crosses =
      start.y > point.y !== end.y > point.y &&
      point.x <
        ((end.x - start.x) * (point.y - start.y)) / (end.y - start.y) + start.x;
    if (crosses) {
      inside = !inside;
    }
  }

  return inside ? "inside" : "outside";
}

function shareInteriorBesideEdge(
  left: readonly Point[],
  right: readonly Point[],
  edgeStart: Point,
  edgeEnd: Point
) {
  const midpoint = {
    x: (edgeStart.x + edgeEnd.x) / 2,
    y: (edgeStart.y + edgeEnd.y) / 2,
  };
  const dx = edgeEnd.x - edgeStart.x;
  const dy = edgeEnd.y - edgeStart.y;
  const length = Math.hypot(dx, dy);
  if (length <= EPSILON) {
    return false;
  }

  const offset = Math.max(EPSILON * 10, length * 0.00001);
  const normalX = (-dy / length) * offset;
  const normalY = (dx / length) * offset;
  const first = { x: midpoint.x + normalX, y: midpoint.y + normalY };
  const second = { x: midpoint.x - normalX, y: midpoint.y - normalY };

  return (
    (classifyPoint(first, left) === "inside" &&
      classifyPoint(first, right) === "inside") ||
    (classifyPoint(second, left) === "inside" &&
      classifyPoint(second, right) === "inside")
  );
}

function getCollinearOverlap(
  leftStart: Point,
  leftEnd: Point,
  rightStart: Point,
  rightEnd: Point
): [Point, Point] | null {
  if (
    Math.abs(cross(leftStart, leftEnd, rightStart)) > EPSILON ||
    Math.abs(cross(leftStart, leftEnd, rightEnd)) > EPSILON
  ) {
    return null;
  }

  const sharedPoints = [leftStart, leftEnd, rightStart, rightEnd].filter(
    (point, index, points) =>
      isPointOnSegment(point, leftStart, leftEnd) &&
      isPointOnSegment(point, rightStart, rightEnd) &&
      points.findIndex((candidate) => pointsEqual(candidate, point)) === index
  );
  if (sharedPoints.length < 2) {
    return null;
  }

  const useX =
    Math.abs(leftEnd.x - leftStart.x) >= Math.abs(leftEnd.y - leftStart.y);
  const ordered = [...sharedPoints].sort((left, right) =>
    useX ? left.x - right.x : left.y - right.y
  );
  const overlapStart = ordered[0];
  const overlapEnd = ordered[ordered.length - 1];

  return squaredDistance(overlapStart, overlapEnd) > EPSILON * EPSILON
    ? [overlapStart, overlapEnd]
    : null;
}

function segmentsCrossProperly(a: Point, b: Point, c: Point, d: Point) {
  return (
    haveOppositeSigns(cross(a, b, c), cross(a, b, d)) &&
    haveOppositeSigns(cross(c, d, a), cross(c, d, b))
  );
}

function isPointOnSegment(point: Point, start: Point, end: Point) {
  if (Math.abs(cross(start, end, point)) > EPSILON) {
    return false;
  }

  return (
    point.x >= Math.min(start.x, end.x) - EPSILON &&
    point.x <= Math.max(start.x, end.x) + EPSILON &&
    point.y >= Math.min(start.y, end.y) - EPSILON &&
    point.y <= Math.max(start.y, end.y) + EPSILON
  );
}

function cross(start: Point, end: Point, point: Point) {
  return (
    (end.x - start.x) * (point.y - start.y) -
    (end.y - start.y) * (point.x - start.x)
  );
}

function haveOppositeSigns(left: number, right: number) {
  return (
    (left > EPSILON && right < -EPSILON) || (left < -EPSILON && right > EPSILON)
  );
}

function pointsEqual(left: Point, right: Point) {
  return (
    Math.abs(left.x - right.x) <= EPSILON &&
    Math.abs(left.y - right.y) <= EPSILON
  );
}

function squaredDistance(left: Point, right: Point) {
  const dx = left.x - right.x;
  const dy = left.y - right.y;
  return dx * dx + dy * dy;
}
