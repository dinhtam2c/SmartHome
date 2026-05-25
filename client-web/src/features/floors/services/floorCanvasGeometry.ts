import type { Point } from "../types/floorTypes";

export type LineSegment = {
  start: Point;
  end: Point;
};

export function getDistance(left: Point, right: Point) {
  return Math.hypot(left.x - right.x, left.y - right.y);
}

export function createLineSegments(points: Point[], closed: boolean): LineSegment[] {
  if (points.length < 2) return [];

  const segments: LineSegment[] = [];
  for (let index = 1; index < points.length; index += 1) {
    segments.push({ start: points[index - 1], end: points[index] });
  }
  if (closed && points.length >= 3) {
    segments.push({ start: points[points.length - 1], end: points[0] });
  }
  return segments;
}

function projectPointToSegment(point: Point, segment: LineSegment): Point {
  const dx = segment.end.x - segment.start.x;
  const dy = segment.end.y - segment.start.y;
  const lengthSquared = dx * dx + dy * dy;
  if (lengthSquared <= Number.EPSILON) return segment.start;

  const projection =
    ((point.x - segment.start.x) * dx + (point.y - segment.start.y) * dy) /
    lengthSquared;
  const ratio = Math.max(0, Math.min(1, projection));
  return {
    x: segment.start.x + ratio * dx,
    y: segment.start.y + ratio * dy,
  };
}

export function snapPointToGeometry(
  point: Point,
  priorityPoints: Point[],
  candidatePoints: Point[],
  candidateSegments: LineSegment[],
  priorityPointThreshold: number,
  pointThreshold: number,
  segmentThreshold: number
) {
  let nearestPriorityPoint: Point | null = null;
  let nearestPriorityPointDistance = Number.POSITIVE_INFINITY;
  priorityPoints.forEach((candidate) => {
    const distance = getDistance(point, candidate);
    if (distance <= priorityPointThreshold && distance < nearestPriorityPointDistance) {
      nearestPriorityPoint = candidate;
      nearestPriorityPointDistance = distance;
    }
  });
  if (nearestPriorityPoint) return nearestPriorityPoint;

  let nearestPoint: Point | null = null;
  let nearestPointDistance = Number.POSITIVE_INFINITY;
  candidatePoints.forEach((candidate) => {
    const distance = getDistance(point, candidate);
    if (distance <= pointThreshold && distance < nearestPointDistance) {
      nearestPoint = candidate;
      nearestPointDistance = distance;
    }
  });
  if (nearestPoint) return nearestPoint;

  let nearestSegmentProjection: Point | null = null;
  let nearestSegmentDistance = Number.POSITIVE_INFINITY;
  candidateSegments.forEach((segment) => {
    const projected = projectPointToSegment(point, segment);
    const distance = getDistance(point, projected);
    if (distance <= segmentThreshold && distance < nearestSegmentDistance) {
      nearestSegmentProjection = projected;
      nearestSegmentDistance = distance;
    }
  });

  return nearestSegmentProjection ?? point;
}
