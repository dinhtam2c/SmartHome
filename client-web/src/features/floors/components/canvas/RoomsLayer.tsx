import type { KonvaEventObject } from "konva/lib/Node";
import { useState } from "react";
import { Circle, Layer, Line, Text } from "react-konva";
import { flattenPoints, getPolygonCentroid } from "../../services/floorGeometry";
import type { CanvasRoom, Point } from "../../types/floorTypes";

type Props = {
  rooms: CanvasRoom[];
  selectedRoomId: string | null;
  canvasOffset: Point;
  canvasWidth: number;
  canvasHeight: number;
  drawingPoints: Point[];
  mousePos: Point | null;
  isClosePreviewActive: boolean;
  showCanvasBoundary: boolean;
  roomLabelMode: "always" | "hover";
  isInteractive: boolean;
  onRoomClick: (roomId: string, event: KonvaEventObject<MouseEvent | TouchEvent>) => void;
};

type RoomShapeProps = {
  room: CanvasRoom;
  isSelected: boolean;
  isInteractive: boolean;
  showLabel: boolean;
  enableHoverLabel: boolean;
  onHoverChange: (roomId: string | null) => void;
  onRoomClick: (roomId: string, event: KonvaEventObject<MouseEvent | TouchEvent>) => void;
};

function RoomShape({
  room,
  isSelected,
  isInteractive,
  showLabel,
  enableHoverLabel,
  onHoverChange,
  onRoomClick,
}: RoomShapeProps) {
  const centroid = getPolygonCentroid(room.polygon);
  const xValues = room.polygon.map((point) => point.x);
  const minX = Math.min(...xValues);
  const maxX = Math.max(...xValues);
  const labelWidth = Math.max(Math.min(maxX - minX, 160), 72);
  const fillColor = room.fillColor ?? "#dcebf8";
  const isListening = isInteractive || enableHoverLabel;

  const handleClick = (event: KonvaEventObject<MouseEvent | TouchEvent>) => {
    if (!isInteractive) {
      return;
    }

    event.cancelBubble = true;
    onRoomClick(room.id, event);
  };

  return (
    <>
      <Line
        points={flattenPoints(room.polygon)}
        closed
        fill={fillColor}
        opacity={isSelected ? 0.88 : 0.72}
        stroke={isSelected ? "#1f6fa4" : "#8eb6d8"}
        strokeWidth={isSelected ? 4 : 2}
        lineJoin="round"
        listening={isListening}
        onClick={handleClick}
        onTap={handleClick}
        onMouseEnter={() => {
          if (enableHoverLabel) {
            onHoverChange(room.id);
          }
        }}
        onMouseLeave={() => {
          if (enableHoverLabel) {
            onHoverChange(null);
          }
        }}
        hitStrokeWidth={20}
      />
      {showLabel ? (
        <Text
          x={centroid.x - labelWidth / 2}
          y={centroid.y - 18}
          width={labelWidth}
          height={28}
          align="center"
          text={room.name}
          fontSize={13}
          fontStyle={isSelected ? "bold" : "normal"}
          fill="#183249"
          ellipsis
          listening={false}
          verticalAlign="middle"
          wrap="none"
        />
      ) : null}
    </>
  );
}

export function RoomsLayer({
  rooms,
  selectedRoomId,
  canvasOffset,
  canvasWidth,
  canvasHeight,
  drawingPoints,
  mousePos,
  isClosePreviewActive,
  showCanvasBoundary,
  roomLabelMode,
  isInteractive,
  onRoomClick,
}: Props) {
  const [hoveredRoomId, setHoveredRoomId] = useState<string | null>(null);
  const enableHoverLabel = roomLabelMode === "hover";
  const boundaryPoints: Point[] = [
    { x: 0, y: 0 },
    { x: canvasWidth, y: 0 },
    { x: canvasWidth, y: canvasHeight },
    { x: 0, y: canvasHeight },
  ];

  const boundaryLines = [
    [boundaryPoints[0], boundaryPoints[1]],
    [boundaryPoints[1], boundaryPoints[2]],
    [boundaryPoints[2], boundaryPoints[3]],
    [boundaryPoints[3], boundaryPoints[0]],
  ] as const;

  const previewPoints =
    drawingPoints.length > 0
      ? [
        ...flattenPoints(drawingPoints),
        ...(mousePos ? [mousePos.x, mousePos.y] : []),
      ]
      : [];
  const previewPolygonPoints =
    drawingPoints.length >= 3
      ? flattenPoints(mousePos ? [...drawingPoints, mousePos] : drawingPoints)
      : [];

  return (
    <Layer x={canvasOffset.x} y={canvasOffset.y}>
      {showCanvasBoundary
        ? boundaryLines.map(([start, end], index) => (
          <Line
            key={`boundary-line-${index}`}
            points={[start.x, start.y, end.x, end.y]}
            stroke="rgba(31, 111, 164, 0.72)"
            strokeWidth={2}
            dash={[8, 6]}
            lineCap="square"
            listening={false}
          />
        ))
        : null}

      {rooms.map((room) => (
        <RoomShape
          key={room.id}
          room={room}
          isSelected={room.id === selectedRoomId}
          isInteractive={isInteractive}
          showLabel={
            roomLabelMode === "always" ||
            room.id === selectedRoomId ||
            room.id === hoveredRoomId
          }
          enableHoverLabel={enableHoverLabel}
          onHoverChange={setHoveredRoomId}
          onRoomClick={onRoomClick}
        />
      ))}

      {previewPolygonPoints.length >= 6 ? (
        <Line
          points={previewPolygonPoints}
          closed
          fill="rgba(75, 146, 204, 0.08)"
          stroke="rgba(31, 111, 164, 0.28)"
          strokeWidth={1}
          listening={false}
        />
      ) : null}

      {previewPoints.length >= 2 ? (
        <Line
          points={previewPoints}
          stroke={isClosePreviewActive ? "#39a66d" : "#1f6fa4"}
          strokeWidth={3}
          dash={isClosePreviewActive ? [] : [10, 6]}
          lineCap="round"
          listening={false}
        />
      ) : null}

      {drawingPoints.map((point, index) => (
        <Circle
          key={`${point.x}:${point.y}:${index}`}
          x={point.x}
          y={point.y}
          radius={index === 0 && isClosePreviewActive ? 8 : 5}
          fill={index === 0 && isClosePreviewActive ? "#39a66d" : "#1f6fa4"}
          stroke="#ffffff"
          strokeWidth={2}
          listening={false}
        />
      ))}

      {mousePos ? (
        <Circle
          x={mousePos.x}
          y={mousePos.y}
          radius={isClosePreviewActive ? 7 : 5}
          fill="#ffffff"
          stroke={isClosePreviewActive ? "#39a66d" : "#1f6fa4"}
          strokeWidth={2}
          listening={false}
        />
      ) : null}
    </Layer>
  );
}
