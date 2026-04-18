import {
  buildCapabilityRegistryMap,
  getCapabilityRegistryCached,
  getCapabilityRegistryKey,
  toEndpointKey,
} from "@/features/capabilities";
import type { CapabilityRole } from "@/features/shared/capabilityRoleUtils";
import { api } from "@/services/http";
import type {
  DeviceCapabilityHistoryPointDto,
  DeviceCommandExecutionDto,
  DeviceCommandRequest,
  DeviceCreateRequest,
  DeviceCreateResponse,
  DeviceDetailDto,
  DeviceEndpointCapabilityRuntimeDto,
  DeviceEndpointDto,
  DeviceRoomAssignRequest,
  DeviceUpdateRequest,
  PagedResult,
} from "./devices.types";

const basePath = "/devices";

type DeviceEndpointApiDto = {
  id: string;
  endpointId: string;
  name: string | null;
  capabilities: DeviceEndpointCapabilityRuntimeDto[];
};

export type DeviceDetailApiDto = Omit<DeviceDetailDto, "capabilities" | "endpoints"> & {
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

  const capabilities: DeviceDetailDto["capabilities"] = endpoints.flatMap((endpoint) => {
    const endpointKey = toEndpointKey(endpoint.endpointId);

    return endpoint.capabilities.map((capability) => {
      const registryEntry = registryMap.get(
        getCapabilityRegistryKey(capability.capabilityId, capability.capabilityVersion)
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
  });

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

export function getDeviceCommands(
  deviceId: string,
  params?: {
    endpointId?: string;
    capabilityId?: string;
    correlationId?: string;
    status?: DeviceCommandExecutionDto["status"];
    from?: number;
    to?: number;
    page?: number;
    pageSize?: number;
  }
) {
  const query = new URLSearchParams();

  if (params?.endpointId) query.set("endpointId", params.endpointId);
  if (params?.capabilityId) query.set("capabilityId", params.capabilityId);
  if (params?.correlationId) query.set("correlationId", params.correlationId);
  if (params?.status) query.set("status", params.status);
  if (typeof params?.from === "number") query.set("from", String(params.from));
  if (typeof params?.to === "number") query.set("to", String(params.to));
  if (typeof params?.page === "number") query.set("page", String(params.page));
  if (typeof params?.pageSize === "number") query.set("pageSize", String(params.pageSize));

  const suffix = query.toString() ? `?${query.toString()}` : "";

  return api<PagedResult<DeviceCommandExecutionDto>>(
    `${basePath}/${deviceId}/commands/history${suffix}`
  );
}

export function getDeviceCapabilityHistory(
  deviceId: string,
  params?: {
    endpointId?: string;
    capabilityId?: string;
    from?: number;
    to?: number;
    page?: number;
    pageSize?: number;
  }
) {
  const query = new URLSearchParams();

  if (params?.endpointId) {
    query.set("endpointId", params.endpointId);
  }

  if (params?.capabilityId) {
    query.set("capabilityId", params.capabilityId);
  }

  if (typeof params?.from === "number") {
    query.set("from", String(params.from));
  }

  if (typeof params?.to === "number") {
    query.set("to", String(params.to));
  }

  if (typeof params?.page === "number") {
    query.set("page", String(params.page));
  }

  if (typeof params?.pageSize === "number") {
    query.set("pageSize", String(params.pageSize));
  }

  const suffix = query.toString() ? `?${query.toString()}` : "";

  return api<PagedResult<DeviceCapabilityHistoryPointDto>>(
    `${basePath}/${deviceId}/capabilities/history${suffix}`
  ).then((result) => ({
    ...result,
    items: result.items.map((item) => ({
      ...item,
      state: (() => {
        try {
          return JSON.parse(item.statePayload) as unknown;
        } catch {
          return item.statePayload;
        }
      })(),
    })),
  }));
}

export function createDevice(request: DeviceCreateRequest) {
  return api<DeviceCreateResponse>(basePath, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function sendDeviceCommand(deviceId: string, request: DeviceCommandRequest) {
  return api<void>(`${basePath}/${deviceId}/commands`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function assignDeviceRoom(deviceId: string, request: DeviceRoomAssignRequest) {
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
