import { useCallback, useMemo } from "react";
import {
  hasInteriorOverlap,
  isPointInsidePolygon,
  segmentEntersPolygon,
} from "../services/floorPolygonOverlap";
import type { CanvasRoom, Point } from "../types/floorTypes";

type Params = {
  rooms: CanvasRoom[];
  drawingPoints: Point[];
  previewPoint: Point | null;
  isClosing: boolean;
};

export function useRoomDrawingOverlap({
  rooms,
  drawingPoints,
  previewPoint,
  isClosing,
}: Params) {
  const roomPolygons = useMemo(
    () => rooms.map((room) => room.polygon),
    [rooms]
  );

  const polygonOverlapsRoom = useCallback(
    (polygon: Point[]) =>
      roomPolygons.some((roomPolygon) =>
        hasInteriorOverlap(roomPolygon, polygon)
      ),
    [roomPolygons]
  );

  const segmentEntersRoom = useCallback(
    (nextPoint: Point) => {
      if (drawingPoints.length === 0) {
        return roomPolygons.some((polygon) =>
          isPointInsidePolygon(nextPoint, polygon)
        );
      }

      const start = drawingPoints[drawingPoints.length - 1];
      return roomPolygons.some((polygon) =>
        segmentEntersPolygon(start, nextPoint, polygon)
      );
    },
    [drawingPoints, roomPolygons]
  );

  const previewEndpoint = isClosing ? drawingPoints[0] : previewPoint;
  const hasOverlap =
    (previewEndpoint !== null && segmentEntersRoom(previewEndpoint)) ||
    (isClosing && polygonOverlapsRoom(drawingPoints));

  return {
    hasOverlap,
    polygonOverlapsRoom,
    segmentEntersRoom,
  };
}
