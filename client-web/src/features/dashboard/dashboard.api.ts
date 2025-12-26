import { api } from "@/services/http";
import type {
  DashboardHomeListItemDto,
  DashboardHomeDto,
  DashboardLocationDto,
  DashboardDeviceDto,
  DeviceCommandRequest,
} from "./dashboard.types";

const basePath = "/dashboard";

export function getHomes() {
  return api<DashboardHomeListItemDto[]>(`${basePath}/homes`);
}

export function getDashboardHome(homeId: string) {
  return api<DashboardHomeDto>(`${basePath}/home/${homeId}`);
}

export function getDashboardLocation(locationId: string) {
  return api<DashboardLocationDto>(`${basePath}/location/${locationId}`);
}

export function getDashboardDevice(deviceId: string) {
  return api<DashboardDeviceDto>(`${basePath}/device/${deviceId}`);
}

export function sendDeviceCommand(
  deviceId: string,
  request: DeviceCommandRequest
) {
  return api<void>(`/devices/${deviceId}/commands`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}
