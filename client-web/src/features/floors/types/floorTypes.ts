import type { SelectableDeviceDto } from "@/features/capabilities";

export interface Point {
  x: number;
  y: number;
}

export interface FloorPlanRoom {
  id: string;
  roomId: string;
  polygon: Point[];
  fillColor: string | null;
}

export interface FloorDevicePlacement {
  id: string;
  deviceId: string;
  x: number;
  y: number;
}

export interface Floor {
  id: string;
  homeId: string;
  name: string;
  sortOrder: number;
  canvasWidth: number;
  canvasHeight: number;
  createdAt: number;
  updatedAt: number;
  floorPlanRooms: FloorPlanRoom[];
  devicePlacements: FloorDevicePlacement[];
}

export interface FloorSummary {
  id: string;
  homeId: string;
  name: string;
  sortOrder: number;
  canvasWidth: number;
  canvasHeight: number;
  createdAt: number;
  updatedAt: number;
  floorPlanRoomCount: number;
  devicePlacementCount: number;
  mappedRoomIds: string[];
  placedDeviceIds: string[];
}

export type FloorHomeDevice = SelectableDeviceDto;

export interface CanvasRoom extends FloorPlanRoom {
  name: string;
}

export interface CanvasDevice extends FloorDevicePlacement {
  displayName: string;
  deviceSnapshot: FloorHomeDevice;
}

export type EditorMode = "view" | "place-device" | "draw-room";

export interface DrawingState {
  points: Point[];
  mousePos: Point | null;
}

export interface RoomFormDraft {
  floorPlanRoomId: string | null;
  roomId: string;
  fillColor: string;
  polygon: Point[];
}

export interface FloorInfoDraft {
  name: string;
  canvasWidth: string;
  canvasHeight: string;
}

export type FloorUpdatedReason =
  | "Created"
  | "Deleted"
  | "InfoUpdated"
  | "RoomAdded"
  | "RoomUpdated"
  | "RoomRemoved"
  | "DevicePlaced"
  | "DeviceMoved"
  | "DeviceRemoved"
  | "Unknown";
