import {
  DEFAULT_ROOM_COLOR,
  MIN_CANVAS_HEIGHT,
  MIN_CANVAS_WIDTH,
} from "./floorConstants";
import type { FloorUpdatedReason, Point, RoomFormDraft } from "../types/floorTypes";

export function normalizeFloorReason(
  value: string | null | undefined
): FloorUpdatedReason {
  switch (value) {
    case "Created":
    case "Deleted":
    case "InfoUpdated":
    case "RoomAdded":
    case "RoomUpdated":
    case "RoomRemoved":
    case "DevicePlaced":
    case "DeviceMoved":
    case "DeviceRemoved":
      return value;
    default:
      return "Unknown";
  }
}

function parseCanvasSize(value: string, minimum: number) {
  const parsed = Number.parseInt(value, 10);

  if (!Number.isFinite(parsed) || parsed < minimum) {
    return null;
  }

  return parsed;
}

export function parseFloorCanvasSize(width: string, height: string) {
  return {
    canvasWidth: parseCanvasSize(width, MIN_CANVAS_WIDTH),
    canvasHeight: parseCanvasSize(height, MIN_CANVAS_HEIGHT),
  };
}

export function createEmptyRoomDraft(polygon: Point[], roomId = ""): RoomFormDraft {
  return {
    floorPlanRoomId: null,
    roomId,
    fillColor: DEFAULT_ROOM_COLOR,
    polygon,
  };
}
