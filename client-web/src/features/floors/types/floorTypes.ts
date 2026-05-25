import type { BuilderDeviceDto } from "@/features/capability-builder";

export interface Point {
  x: number;
  y: number;
}

export interface FloorRoom {
  id: string;
  linkedRoomId: string | null;
  linkedRoomName: string | null;
  label: string;
  polygon: Point[];
  fillColor: string | null;
}

export interface PlacedFloorDevice {
  id: string;
  deviceId: string;
  deviceName: string | null;
  isOnline: boolean;
  isDeleted: boolean;
  floorRoomId: string | null;
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
  rooms: FloorRoom[];
  placedFloorDevices: PlacedFloorDevice[];
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
  roomCount: number;
  placedDeviceCount: number;
  placedDeviceIds: string[];
}

export type FloorHomeDevice = BuilderDeviceDto;

export interface CanvasDevice extends PlacedFloorDevice {
  displayName: string;
  deviceSnapshot: FloorHomeDevice | null;
}

export type EditorMode = "view" | "place-device" | "draw-room";

export interface DrawingState {
  points: Point[];
  mousePos: Point | null;
}

export interface RoomFormDraft {
  roomId: string | null;
  linkedRoomId: string;
  label: string;
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
  | "LinkedRoomDeleted"
  | "DeviceDeleted"
  | "Unknown";
