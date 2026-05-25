import type { Point } from "../types/floorTypes";

const EPSILON = 0.000001;

export function clampPoint(
  point: Point,
  canvasWidth: number,
  canvasHeight: number,
  padding = 0
): Point {
  return {
    x: Math.min(Math.max(point.x, padding), Math.max(padding, canvasWidth - padding)),
    y: Math.min(Math.max(point.y, padding), Math.max(padding, canvasHeight - padding)),
  };
}

export function flattenPoints(points: Point[]) {
  return points.flatMap((point) => [point.x, point.y]);
}

export function getPolygonCentroid(points: Point[]) {
  if (points.length === 0) {
    return { x: 0, y: 0 };
  }

  let area = 0;
  let x = 0;
  let y = 0;

  for (let index = 0; index < points.length; index += 1) {
    const current = points[index];
    const next = points[(index + 1) % points.length];
    const cross = current.x * next.y - next.x * current.y;

    area += cross;
    x += (current.x + next.x) * cross;
    y += (current.y + next.y) * cross;
  }

  const normalizedArea = area / 2;

  if (Math.abs(normalizedArea) < EPSILON) {
    const total = points.reduce(
      (accumulator, point) => ({
        x: accumulator.x + point.x,
        y: accumulator.y + point.y,
      }),
      { x: 0, y: 0 }
    );

    return {
      x: total.x / points.length,
      y: total.y / points.length,
    };
  }

  return {
    x: x / (6 * normalizedArea),
    y: y / (6 * normalizedArea),
  };
}
