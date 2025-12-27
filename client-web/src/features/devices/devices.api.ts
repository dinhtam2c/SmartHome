import { api } from "@/services/http";
import type {
  DeviceListElement,
  DeviceDetails,
  DeviceAddRequest,
  DeviceAddResponse,
  DeviceLocationAssignRequest,
  DeviceGatewayAssignRequest,
  DeviceUpdateRequest,
} from "./devices.types";

const basePath = "/devices";

export function getDevices() {
  return api<DeviceListElement[]>(`${basePath}`);
}

export function getDeviceDetails(deviceId: string) {
  return api<DeviceDetails>(`${basePath}/${deviceId}`);
}

export function addDevice(request: DeviceAddRequest) {
  return api<DeviceAddResponse>(`${basePath}`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function assignLocationToDevice(
  deviceId: string,
  request: DeviceLocationAssignRequest
) {
  return api(`${basePath}/${deviceId}/location`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function assignGatewayToDevice(
  deviceId: string,
  request: DeviceGatewayAssignRequest
) {
  return api(`${basePath}/${deviceId}/gateway`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function deleteDevice(deviceId: string) {
  return api(`${basePath}/${deviceId}`, {
    method: "DELETE",
  });
}

export function updateDevice(deviceId: string, request: DeviceUpdateRequest) {
  return api(`${basePath}/${deviceId}`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}
