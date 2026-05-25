import { api } from "@/shared/api/http";
import type { Floor, FloorSummary, Point } from "../types/floorTypes";

const basePath = (homeId: string) => `/homes/${homeId}/floors`;

type FloorInfoRequest = {
  name?: string;
  canvasWidth?: number;
  canvasHeight?: number;
};

type UpsertFloorRoomRequest = {
  linkedRoomId?: string | null;
  label: string;
  polygon: Point[];
  fillColor?: string | null;
};

type PlaceDeviceRequest = {
  deviceId: string;
  x: number;
  y: number;
  floorRoomId?: string | null;
};

type MoveDeviceRequest = {
  x: number;
  y: number;
  floorRoomId?: string | null;
};

export const floorsApi = {
  list(homeId: string) {
    return api<FloorSummary[]>(basePath(homeId));
  },

  get(homeId: string, floorId: string) {
    return api<Floor>(`${basePath(homeId)}/${floorId}`);
  },

  create(
    homeId: string,
    data: {
      name: string;
      canvasWidth: number;
      canvasHeight: number;
    }
  ) {
    return api<{ id: string }>(basePath(homeId), {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  reorder(homeId: string, floorIds: string[]) {
    return api<void>(`${basePath(homeId)}/order`, {
      method: "PUT",
      body: JSON.stringify({ floorIds }),
    });
  },

  updateInfo(homeId: string, floorId: string, data: FloorInfoRequest) {
    return api<void>(`${basePath(homeId)}/${floorId}`, {
      method: "PATCH",
      body: JSON.stringify(data),
    });
  },

  delete(homeId: string, floorId: string) {
    return api<void>(`${basePath(homeId)}/${floorId}`, {
      method: "DELETE",
    });
  },

  createRoom(homeId: string, floorId: string, data: UpsertFloorRoomRequest) {
    return api<{ id: string }>(`${basePath(homeId)}/${floorId}/rooms`, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  updateRoom(homeId: string, floorId: string, roomId: string, data: UpsertFloorRoomRequest) {
    return api<{ id: string }>(`${basePath(homeId)}/${floorId}/rooms/${roomId}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  removeRoom(homeId: string, floorId: string, roomId: string) {
    return api<void>(`${basePath(homeId)}/${floorId}/rooms/${roomId}`, {
      method: "DELETE",
    });
  },

  placeDevice(homeId: string, floorId: string, data: PlaceDeviceRequest) {
    return api<{ id: string }>(`${basePath(homeId)}/${floorId}/devices`, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  moveDevice(homeId: string, floorId: string, placedFloorDeviceId: string, data: MoveDeviceRequest) {
    return api<void>(`${basePath(homeId)}/${floorId}/devices/${placedFloorDeviceId}`, {
      method: "PATCH",
      body: JSON.stringify(data),
    });
  },

  removePlacedFloorDevice(homeId: string, floorId: string, placedFloorDeviceId: string) {
    return api<void>(`${basePath(homeId)}/${floorId}/devices/${placedFloorDeviceId}`, {
      method: "DELETE",
    });
  },
};
