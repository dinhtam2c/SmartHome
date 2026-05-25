import { useCallback, useEffect, useMemo, useRef } from "react";
import type { KonvaEventObject } from "konva/lib/Node";
import { Stage } from "react-konva";
import { FLOOR_DEVICE_DRAG_TYPE } from "../../services/floorConstants";
import { clampPoint } from "../../services/floorGeometry";
import { useCanvasScale } from "../../hooks/useCanvasScale";
import { DevicesLayer } from "./DevicesLayer";
import { RoomsLayer } from "./RoomsLayer";
import type { CanvasDevice, Floor, Point } from "../../types/floorTypes";
import type { useFloorEditor } from "../../hooks/useFloorEditor";
import styles from "./FloorCanvas.module.css";

type EditorState = ReturnType<typeof useFloorEditor>;

type Props = {
  floor: Floor;
  devices: CanvasDevice[];
  editor: EditorState;
  pendingPlacementDeviceId: string | null;
  onBlankClick: () => void;
  onCreateRoom: (polygon: Point[]) => void;
  onFinishDrawingRoom: () => void;
  onCancelDrawingRoom: () => void;
  onUndoDrawingPoint: () => void;
  onPlaceDevice: (deviceId: string, point: Point) => void;
  onRoomClick: (roomId: string) => void;
  onDeviceClick: (placedFloorDeviceId: string) => void;
  onDeviceDragEnd: (placedFloorDeviceId: string, point: Point) => void;
};

const DEVICE_EDGE_PADDING = 3;
const CANVAS_HIT_GUTTER_PX = 34;
const MIN_SCALE = 0.001;
const SNAP_CORNER_THRESHOLD_PX = 10;
const SNAP_POINT_THRESHOLD_PX = 10;
const SNAP_SEGMENT_THRESHOLD_PX = 8;
const CLOSE_POLYGON_THRESHOLD_PX = 28;
const DUPLICATE_POINT_THRESHOLD_PX = 1.2;

type LineSegment = {
  start: Point;
  end: Point;
};

function getDistance(left: Point, right: Point) {
  return Math.hypot(left.x - right.x, left.y - right.y);
}

function createLineSegments(points: Point[], closed: boolean): LineSegment[] {
  if (points.length < 2) {
    return [];
  }

  const segments: LineSegment[] = [];

  for (let index = 1; index < points.length; index += 1) {
    segments.push({
      start: points[index - 1],
      end: points[index],
    });
  }

  if (closed && points.length >= 3) {
    segments.push({
      start: points[points.length - 1],
      end: points[0],
    });
  }

  return segments;
}

function projectPointToSegment(point: Point, segment: LineSegment): Point {
  const dx = segment.end.x - segment.start.x;
  const dy = segment.end.y - segment.start.y;
  const lengthSquared = dx * dx + dy * dy;

  if (lengthSquared <= Number.EPSILON) {
    return segment.start;
  }

  const projection =
    ((point.x - segment.start.x) * dx + (point.y - segment.start.y) * dy) /
    lengthSquared;
  const ratio = Math.max(0, Math.min(1, projection));

  return {
    x: segment.start.x + ratio * dx,
    y: segment.start.y + ratio * dy,
  };
}

function snapPointToGeometry(
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

    if (
      distance <= priorityPointThreshold &&
      distance < nearestPriorityPointDistance
    ) {
      nearestPriorityPoint = candidate;
      nearestPriorityPointDistance = distance;
    }
  });

  if (nearestPriorityPoint) {
    return nearestPriorityPoint;
  }

  let nearestPoint: Point | null = null;
  let nearestPointDistance = Number.POSITIVE_INFINITY;

  candidatePoints.forEach((candidate) => {
    const distance = getDistance(point, candidate);

    if (distance <= pointThreshold && distance < nearestPointDistance) {
      nearestPoint = candidate;
      nearestPointDistance = distance;
    }
  });

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

  if (nearestPoint) {
    return nearestPoint;
  }

  if (nearestSegmentProjection) {
    return nearestSegmentProjection;
  }

  return point;
}

export function FloorCanvas({
  floor,
  devices,
  editor,
  pendingPlacementDeviceId,
  onBlankClick,
  onCreateRoom,
  onFinishDrawingRoom,
  onCancelDrawingRoom,
  onUndoDrawingPoint,
  onPlaceDevice,
  onRoomClick,
  onDeviceClick,
  onDeviceDragEnd,
}: Props) {
  const { containerRef, scale, scaledWidth, scaledHeight } = useCanvasScale(
    floor.canvasWidth,
    floor.canvasHeight,
    CANVAS_HIT_GUTTER_PX
  );
  const stageHostRef = useRef<HTMLDivElement>(null);
  const normalizedScale = Math.max(scale, MIN_SCALE);
  const canvasHitGutter = CANVAS_HIT_GUTTER_PX / normalizedScale;
  const cornerSnapThreshold = SNAP_CORNER_THRESHOLD_PX / normalizedScale;
  const pointSnapThreshold = SNAP_POINT_THRESHOLD_PX / normalizedScale;
  const segmentSnapThreshold = SNAP_SEGMENT_THRESHOLD_PX / normalizedScale;
  const closePolygonThreshold = CLOSE_POLYGON_THRESHOLD_PX / normalizedScale;
  const duplicatePointThreshold = DUPLICATE_POINT_THRESHOLD_PX / normalizedScale;
  const drawingPointCount = editor.drawing.points.length;
  const isClosePreviewActive =
    drawingPointCount >= 3 &&
    editor.drawing.mousePos !== null &&
    getDistance(editor.drawing.points[0], editor.drawing.mousePos) <= closePolygonThreshold;

  const boundaryPoints = useMemo<Point[]>(
    () => [
      { x: 0, y: 0 },
      { x: floor.canvasWidth, y: 0 },
      { x: floor.canvasWidth, y: floor.canvasHeight },
      { x: 0, y: floor.canvasHeight },
    ],
    [floor.canvasHeight, floor.canvasWidth]
  );

  const snapPoints = useMemo(
    () => [
      ...floor.rooms.flatMap((room) => room.polygon),
      ...editor.drawing.points,
    ],
    [editor.drawing.points, floor.rooms]
  );

  const snapSegments = useMemo(
    () => [
      ...createLineSegments(boundaryPoints, true),
      ...floor.rooms.flatMap((room) => createLineSegments(room.polygon, true)),
      ...createLineSegments(editor.drawing.points, false),
    ],
    [boundaryPoints, editor.drawing.points, floor.rooms]
  );

  const stageSize = useMemo(
    () => ({
      width: Math.max(scaledWidth, 1),
      height: Math.max(scaledHeight, 1),
    }),
    [scaledHeight, scaledWidth]
  );

  const interactiveStageSize = useMemo(
    () => ({
      width: stageSize.width + CANVAS_HIT_GUTTER_PX * 2,
      height: stageSize.height + CANVAS_HIT_GUTTER_PX * 2,
    }),
    [stageSize.height, stageSize.width]
  );

  const canvasOffset = useMemo(
    () => ({
      x: canvasHitGutter,
      y: canvasHitGutter,
    }),
    [canvasHitGutter]
  );

  const toPointerCanvasPoint = useCallback(
    (pointer: { x: number; y: number; } | null | undefined, padding = 0) => {
      if (!pointer || scale <= 0) {
        return null;
      }

      return clampPoint(
        {
          x: pointer.x / scale - canvasHitGutter,
          y: pointer.y / scale - canvasHitGutter,
        },
        floor.canvasWidth,
        floor.canvasHeight,
        padding
      );
    },
    [canvasHitGutter, floor.canvasHeight, floor.canvasWidth, scale]
  );

  const getSnappedDrawingPoint = useCallback(
    (point: Point) =>
      snapPointToGeometry(
        point,
        boundaryPoints,
        snapPoints,
        snapSegments,
        cornerSnapThreshold,
        pointSnapThreshold,
        segmentSnapThreshold
      ),
    [
      boundaryPoints,
      cornerSnapThreshold,
      pointSnapThreshold,
      segmentSnapThreshold,
      snapPoints,
      snapSegments,
    ]
  );

  const toCanvasPoint = useCallback(
    (clientX: number, clientY: number, padding = 0) => {
      const rect = stageHostRef.current?.getBoundingClientRect();

      if (!rect || scale <= 0) {
        return null;
      }

      return clampPoint(
        {
          x: (clientX - rect.left) / scale - canvasHitGutter,
          y: (clientY - rect.top) / scale - canvasHitGutter,
        },
        floor.canvasWidth,
        floor.canvasHeight,
        padding
      );
    },
    [canvasHitGutter, floor.canvasHeight, floor.canvasWidth, scale]
  );

  const handleStageClick = useCallback(
    (event: KonvaEventObject<MouseEvent | TouchEvent>) => {
      const stage = event.target.getStage();

      if (!stage) {
        return;
      }

      if (editor.mode === "draw-room") {
        const pointer = toPointerCanvasPoint(stage.getPointerPosition());

        if (!pointer) {
          return;
        }

        const nextPoint = getSnappedDrawingPoint(pointer);

        if (
          editor.drawing.points.length > 0 &&
          getDistance(
            editor.drawing.points[editor.drawing.points.length - 1],
            nextPoint
          ) <= duplicatePointThreshold
        ) {
          return;
        }

        if (
          editor.drawing.points.length >= 3 &&
          getDistance(editor.drawing.points[0], nextPoint) <= closePolygonThreshold
        ) {
          const polygon = editor.finishDrawing();

          if (polygon && polygon.length >= 3) {
            onCreateRoom(polygon);
          }

          return;
        }

        editor.addDrawingPoint(nextPoint);
        return;
      }

      if (editor.mode === "place-device") {
        if (pendingPlacementDeviceId && event.target === stage) {
          const pointer = toPointerCanvasPoint(
            stage.getPointerPosition(),
            DEVICE_EDGE_PADDING
          );

          if (pointer) {
            onPlaceDevice(pendingPlacementDeviceId, pointer);
          }

          return;
        }

        if (event.target !== stage) {
          return;
        }

        onBlankClick();
      }
    },
    [
      editor,
      closePolygonThreshold,
      duplicatePointThreshold,
      getSnappedDrawingPoint,
      onBlankClick,
      onCreateRoom,
      onPlaceDevice,
      pendingPlacementDeviceId,
      toPointerCanvasPoint,
    ]
  );

  const handleStageDoubleClick = useCallback(
    (event: KonvaEventObject<MouseEvent | TouchEvent>) => {
      const stage = event.target.getStage();

      if (!stage || editor.mode !== "draw-room") {
        return;
      }

      const polygon = editor.finishDrawing();

      if (!polygon) {
        return;
      }

      const nextPolygon =
        polygon.length >= 4 &&
          getDistance(polygon[polygon.length - 1], polygon[polygon.length - 2]) <
          10 / normalizedScale
          ? polygon.slice(0, -1)
          : polygon;

      if (nextPolygon.length >= 3) {
        onCreateRoom(nextPolygon);
      }
    },
    [editor, normalizedScale, onCreateRoom]
  );

  useEffect(() => {
    if (editor.mode !== "draw-room") {
      return undefined;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      const target = event.target;
      const isTypingTarget =
        target instanceof HTMLInputElement ||
        target instanceof HTMLTextAreaElement ||
        target instanceof HTMLSelectElement;

      if (isTypingTarget) {
        return;
      }

      if (event.key === "Escape") {
        event.preventDefault();
        onCancelDrawingRoom();
        return;
      }

      if (event.key === "Backspace" || event.key === "Delete") {
        if (drawingPointCount > 0) {
          event.preventDefault();
          onUndoDrawingPoint();
        }

        return;
      }

      if (event.key === "Enter" && drawingPointCount >= 3) {
        event.preventDefault();
        onFinishDrawingRoom();
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [
    drawingPointCount,
    editor.mode,
    onCancelDrawingRoom,
    onFinishDrawingRoom,
    onUndoDrawingPoint,
  ]);

  const handleStageMouseMove = useCallback(
    (event: KonvaEventObject<MouseEvent>) => {
      if (editor.mode !== "draw-room") {
        return;
      }

      const stage = event.target.getStage();
      const pointer = stage?.getPointerPosition();

      const canvasPoint = toPointerCanvasPoint(pointer);

      if (!canvasPoint) {
        return;
      }

      editor.updateMousePos(getSnappedDrawingPoint(canvasPoint));
    },
    [editor, getSnappedDrawingPoint, toPointerCanvasPoint]
  );

  const handleStageMouseLeave = useCallback(() => {
    if (editor.mode !== "draw-room") {
      return;
    }

    editor.updateMousePos(null);
  }, [editor]);

  const handleDragOver = useCallback(
    (event: React.DragEvent<HTMLDivElement>) => {
      if (editor.mode !== "place-device") {
        return;
      }

      event.preventDefault();
      event.dataTransfer.dropEffect = "move";
    },
    [editor.mode]
  );

  const handleDrop = useCallback(
    (event: React.DragEvent<HTMLDivElement>) => {
      if (editor.mode !== "place-device") {
        return;
      }

      event.preventDefault();
      const deviceId = event.dataTransfer.getData(FLOOR_DEVICE_DRAG_TYPE);

      if (!deviceId) {
        return;
      }

      const point = toCanvasPoint(event.clientX, event.clientY, DEVICE_EDGE_PADDING);

      if (!point) {
        return;
      }

      onPlaceDevice(deviceId, point);
    },
    [editor.mode, onPlaceDevice, toCanvasPoint]
  );

  return (
    <div ref={containerRef} className={styles.viewport}>
      <div
        ref={stageHostRef}
        className={styles.stageHost}
        style={{
          width: interactiveStageSize.width,
          height: interactiveStageSize.height,
        }}
        onDragOver={handleDragOver}
        onDrop={handleDrop}
      >
        <Stage
          width={interactiveStageSize.width}
          height={interactiveStageSize.height}
          scaleX={scale}
          scaleY={scale}
          onClick={handleStageClick}
          onTap={handleStageClick}
          onDblClick={handleStageDoubleClick}
          onDblTap={handleStageDoubleClick}
          onMouseMove={handleStageMouseMove}
          onMouseLeave={handleStageMouseLeave}
          style={{
            cursor:
              editor.mode === "draw-room"
                ? "crosshair"
                : pendingPlacementDeviceId
                  ? "copy"
                  : editor.mode === "place-device"
                    ? "pointer"
                    : "default",
          }}
        >
          <RoomsLayer
            rooms={floor.rooms}
            selectedRoomId={editor.selectedRoomId}
            canvasOffset={canvasOffset}
            canvasWidth={floor.canvasWidth}
            canvasHeight={floor.canvasHeight}
            drawingPoints={editor.drawing.points}
            mousePos={editor.drawing.mousePos}
            isClosePreviewActive={isClosePreviewActive}
            showCanvasBoundary={editor.mode === "draw-room"}
            roomLabelMode={editor.mode === "view" ? "hover" : "always"}
            isInteractive={editor.mode === "place-device"}
            onRoomClick={(roomId, event) => {
              if (pendingPlacementDeviceId) {
                const stage = event.target.getStage();
                const pointer = toPointerCanvasPoint(
                  stage?.getPointerPosition(),
                  DEVICE_EDGE_PADDING
                );

                if (pointer) {
                  onPlaceDevice(pendingPlacementDeviceId, pointer);
                }

                return;
              }

              onRoomClick(roomId);
            }}
          />
          <DevicesLayer
            devices={devices}
            selectedPlacedFloorDeviceId={editor.selectedPlacedFloorDeviceId}
            canvasOffset={canvasOffset}
            canvasWidth={floor.canvasWidth}
            canvasHeight={floor.canvasHeight}
            dragPadding={DEVICE_EDGE_PADDING}
            interactive={editor.mode !== "draw-room"}
            draggable={editor.mode === "place-device"}
            onDeviceClick={onDeviceClick}
            onDeviceDragEnd={(placedFloorDeviceId, x, y) =>
              onDeviceDragEnd(
                placedFloorDeviceId,
                clampPoint(
                  { x, y },
                  floor.canvasWidth,
                  floor.canvasHeight,
                  DEVICE_EDGE_PADDING
                )
              )
            }
          />
        </Stage>
      </div>
    </div>
  );
}
