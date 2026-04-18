import {
  buildCapabilityRegistryMap,
  getCapabilityRegistryCached,
  getCapabilityRegistryKey,
  toEndpointKey,
} from "@/features/capabilities";
import type { CapabilityRole } from "@/features/shared/capabilityRoleUtils";
import { api } from "@/services/http";
import type {
  HomeCreateRequest,
  HomeCreateResponse,
  HomeDetailDto,
  HomeListItemDto,
  HomeSceneBuilderDeviceDto,
  HomeUnassignedDeviceOverviewDto,
  HomeUpdateRequest,
} from "./homes.types";

const basePath = "/homes";

type HomeCapabilityOverviewApiDto = {
  capabilityId: string;
  capabilityVersion: number;
  supportedOperations: string[] | null;
  lastReportedAt: number;
  state: unknown;
};

type HomeDeviceEndpointApiDto = {
  endpointId: string;
  name?: string | null;
  capabilities: HomeCapabilityOverviewApiDto[] | null;
};

type HomeUnassignedDeviceOverviewApiDto = Omit<
  HomeUnassignedDeviceOverviewDto,
  "endpoints"
> & {
  endpoints: HomeDeviceEndpointApiDto[] | null;
};

type HomeDetailApiDto = Omit<HomeDetailDto, "unassignedDevices"> & {
  scenes: HomeDetailDto["scenes"] | null;
  rooms: HomeDetailDto["rooms"] | null;
  unassignedDevices: HomeUnassignedDeviceOverviewApiDto[] | null;
};

function toCapabilityRole(
  role: CapabilityRole | undefined
): CapabilityRole {
  if (role === "Control" || role === "Sensor" || role === "Actuator") {
    return role;
  }

  return "Unknown";
}

async function normalizeHomeDetail(home: HomeDetailApiDto): Promise<HomeDetailDto> {
  const registryEntries = await getCapabilityRegistryCached();
  const registryMap = buildCapabilityRegistryMap(registryEntries);

  return {
    ...home,
    scenes: Array.isArray(home.scenes) ? home.scenes : [],
    rooms: Array.isArray(home.rooms) ? home.rooms : [],
    unassignedDevices: Array.isArray(home.unassignedDevices)
      ? home.unassignedDevices.map((device) => ({
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
      }))
      : [],
  };
}

export function getHomes() {
  return api<HomeListItemDto[]>(basePath);
}

export function getHomeDetail(homeId: string) {
  return api<HomeDetailApiDto>(`${basePath}/${homeId}`).then(normalizeHomeDetail);
}

export function getHomeDevices(homeId: string, roomId?: string) {
  const query = new URLSearchParams();

  if (roomId && roomId.trim() !== "") {
    query.set("roomId", roomId.trim());
  }

  const suffix = query.toString() ? `?${query.toString()}` : "";
  return api<HomeSceneBuilderDeviceDto[]>(`${basePath}/${homeId}/devices${suffix}`);
}

export function createHome(request: HomeCreateRequest) {
  return api<HomeCreateResponse>(basePath, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateHome(homeId: string, request: HomeUpdateRequest) {
  return api<void>(`${basePath}/${homeId}`, {
    method: "PATCH",
    body: JSON.stringify(request),
  });
}

export function deleteHome(homeId: string) {
  return api<void>(`${basePath}/${homeId}`, {
    method: "DELETE",
  });
}
