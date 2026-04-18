import {
  buildCapabilityRegistryMap,
  getCapabilityRegistryCached,
  getCapabilityRegistryKey,
  toEndpointKey,
} from "@/features/capabilities";
import type { CapabilityRole } from "@/features/shared/capabilityRoleUtils";
import { api } from "@/services/http";
import type {
  RoomCreateRequest,
  RoomCreateResponse,
  RoomDetailDto,
  RoomUpdateRequest,
} from "./rooms.types";

const basePath = "/homes";

type RoomCapabilityOverviewApiDto = {
  capabilityId: string;
  capabilityVersion: number;
  supportedOperations: string[] | null;
  lastReportedAt: number;
  state: unknown;
};

type RoomDeviceEndpointApiDto = {
  endpointId: string;
  name?: string | null;
  capabilities: RoomCapabilityOverviewApiDto[] | null;
};

type RoomDeviceOverviewApiDto = {
  id: string;
  name: string;
  isOnline: boolean;
  endpoints: RoomDeviceEndpointApiDto[] | null;
};

type RoomDetailApiDto = Omit<RoomDetailDto, "devices"> & {
  devices: RoomDeviceOverviewApiDto[];
};

function toCapabilityRole(
  role: CapabilityRole | undefined
): CapabilityRole {
  if (role === "Control" || role === "Sensor" || role === "Actuator") {
    return role;
  }

  return "Unknown";
}

async function normalizeRoomDetail(
  room: RoomDetailApiDto
): Promise<RoomDetailDto> {
  const registryEntries = await getCapabilityRegistryCached();
  const registryMap = buildCapabilityRegistryMap(registryEntries);

  return {
    ...room,
    devices: room.devices.map((device) => ({
      ...device,
      endpoints: Array.isArray(device.endpoints)
        ? device.endpoints.map((endpoint) => {
          const endpointId = toEndpointKey(endpoint.endpointId);

          return {
            endpointId,
            name: endpoint.name ?? null,
            capabilities: Array.isArray(endpoint.capabilities)
              ? endpoint.capabilities.map((capability) => {
                const registryEntry = registryMap.get(
                  getCapabilityRegistryKey(
                    capability.capabilityId,
                    capability.capabilityVersion
                  )
                );

                return {
                  id: `${device.id}:${endpointId}:${capability.capabilityId}:${capability.capabilityVersion}`,
                  capabilityId: capability.capabilityId,
                  capabilityVersion: capability.capabilityVersion,
                  supportedOperations: Array.isArray(capability.supportedOperations)
                    ? capability.supportedOperations
                    : [],
                  lastReportedAt: capability.lastReportedAt,
                  role: toCapabilityRole(registryEntry?.role),
                  metadata: registryEntry?.metadata ?? null,
                  state: capability.state,
                  hasRegistryMetadata: Boolean(registryEntry),
                };
              })
              : [],
          };
        })
        : [],
    })),
  };
}

export async function getRoomDetail(homeId: string, roomId: string) {
  const detail = await api<RoomDetailApiDto>(
    `${basePath}/${homeId}/rooms/${roomId}`
  );

  return normalizeRoomDetail(detail);
}

export function createRoom(homeId: string, request: RoomCreateRequest) {
  return api<RoomCreateResponse>(`${basePath}/${homeId}/rooms`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateRoom(homeId: string, roomId: string, request: RoomUpdateRequest) {
  return api<void>(`${basePath}/${homeId}/rooms/${roomId}`, {
    method: "PATCH",
    body: JSON.stringify(request),
  });
}

export function deleteRoom(homeId: string, roomId: string) {
  return api<void>(`${basePath}/${homeId}/rooms/${roomId}`, {
    method: "DELETE",
  });
}
