import {
  buildCapabilityRegistryMap,
  getCapabilityRegistryCached,
  getCapabilityRegistryKey,
  toEndpointKey,
} from "@/features/capabilities";
import type { CapabilityRole } from "@/features/capabilities";
import { api } from "@/shared/api/http";
import type {
  DeviceCommandRequest,
  DeviceCreateRequest,
  DeviceCreateResponse,
  DeviceDetailDto,
  DeviceEndpointCapabilityRuntimeDto,
  DeviceEndpointDto,
  DeviceRoomAssignRequest,
  DeviceUpdateRequest,
} from "../types/deviceTypes";

const basePath = "/devices";

type DeviceEndpointApiDto = {
  id: string;
  endpointId: string;
  name: string | null;
  capabilities: DeviceEndpointCapabilityRuntimeDto[];
};

export type DeviceDetailApiDto = Omit<
  DeviceDetailDto,
  "capabilities" | "endpoints"
> & {
  endpoints: DeviceEndpointApiDto[];
};

function toCapabilityRole(role: unknown): CapabilityRole {
  if (role === "Control" || role === "Sensor" || role === "Actuator") {
    return role;
  }

  return "Unknown";
}

function normalizeEndpoint(endpoint: DeviceEndpointApiDto): DeviceEndpointDto {
  const endpointId = endpoint.endpointId.trim();

  return {
    id: endpoint.id,
    endpointId,
    name: endpoint.name,
    capabilities: (endpoint.capabilities ?? []).map((capability) => ({
      capabilityId: capability.capabilityId,
      capabilityVersion: capability.capabilityVersion,
      supportedOperations: Array.isArray(capability.supportedOperations)
        ? capability.supportedOperations
        : [],
      lastReportedAt: capability.lastReportedAt,
      state: capability.state,
    })),
  };
}

async function normalizeDeviceDetail(
  device: DeviceDetailApiDto
): Promise<DeviceDetailDto> {
  const registryEntries = await getCapabilityRegistryCached();
  const registryMap = buildCapabilityRegistryMap(registryEntries);
  const endpoints = (device.endpoints ?? []).map(normalizeEndpoint);

  const capabilities: DeviceDetailDto["capabilities"] = endpoints.flatMap(
    (endpoint) => {
      const endpointKey = toEndpointKey(endpoint.endpointId);

      return endpoint.capabilities.map((capability) => {
        const registryEntry = registryMap.get(
          getCapabilityRegistryKey(
            capability.capabilityId,
            capability.capabilityVersion
          )
        );

        return {
          id: `${endpointKey}:${capability.capabilityId}:${capability.capabilityVersion}`,
          endpointKey,
          endpointId: endpoint.endpointId,
          endpointName: endpoint.name,
          capabilityId: capability.capabilityId,
          capabilityVersion: capability.capabilityVersion,
          supportedOperations: capability.supportedOperations,
          lastReportedAt: capability.lastReportedAt,
          state: capability.state,
          hasRegistryMetadata: Boolean(registryEntry),
          role: toCapabilityRole(registryEntry?.role),
          metadata: registryEntry?.metadata ?? null,
          stateSchema: registryEntry?.stateSchema ?? null,
          operations: registryEntry?.operations ?? null,
        };
      });
    }
  );

  return {
    ...device,
    endpoints,
    capabilities,
  };
}

export async function getDeviceDetail(deviceId: string) {
  const device = await api<DeviceDetailApiDto>(`${basePath}/${deviceId}`);
  return normalizeDeviceDetail(device);
}

export function createDevice(request: DeviceCreateRequest) {
  return api<DeviceCreateResponse>(basePath, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function sendDeviceCommand(
  deviceId: string,
  request: DeviceCommandRequest
) {
  return api<void>(`${basePath}/${deviceId}/commands`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function assignDeviceRoom(
  deviceId: string,
  request: DeviceRoomAssignRequest
) {
  return api<void>(`${basePath}/${deviceId}/room`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateDevice(deviceId: string, request: DeviceUpdateRequest) {
  return api<void>(`${basePath}/${deviceId}`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export function deleteDevice(deviceId: string) {
  return api<void>(`${basePath}/${deviceId}`, {
    method: "DELETE",
  });
}
