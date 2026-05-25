import { api } from "@/shared/api/http";
import type { Floor, FloorSummary, Point } from "../types/floorTypes";

const basePath = (homeId: string) => `/homes/${homeId}/floors`;

type FloorInfoRequest = {
  name?: string;
  canvasWidth?: number;
  canvasHeight?: number;
};

type CreateFloorPlanRoomRequest = {
  roomId: string;
  polygon: Point[];
  fillColor?: string | null;
};

type UpdateFloorPlanRoomRequest = Omit<CreateFloorPlanRoomRequest, "roomId">;

type PlaceDeviceRequest = {
  deviceId: string;
  x: number;
  y: number;
};

type MoveDeviceRequest = {
  x: number;
  y: number;
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

  createRoom(homeId: string, floorId: string, data: CreateFloorPlanRoomRequest) {
    return api<{ id: string }>(`${basePath(homeId)}/${floorId}/rooms`, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  updateRoom(
    homeId: string,
    floorId: string,
    floorPlanRoomId: string,
    data: UpdateFloorPlanRoomRequest
  ) {
    return api<void>(`${basePath(homeId)}/${floorId}/rooms/${floorPlanRoomId}`, {
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

  moveDevice(homeId: string, floorId: string, placementId: string, data: MoveDeviceRequest) {
    return api<void>(`${basePath(homeId)}/${floorId}/devices/${placementId}`, {
      method: "PATCH",
      body: JSON.stringify(data),
    });
  },

  removeDevicePlacement(homeId: string, floorId: string, placementId: string) {
    return api<void>(`${basePath(homeId)}/${floorId}/devices/${placementId}`, {
      method: "DELETE",
    });
  },
};
