import type { RealtimeDeltaEvent } from "@/shared/api/sse";
import type {
  DeviceDetailDto,
  DeviceEndpointCapabilityRuntimeDto,
  DeviceEndpointDto,
} from "../types/deviceTypes";

type DeviceDelta = Partial<
  Pick<
    DeviceDetailDto,
    | "category"
    | "firmwareVersion"
    | "homeId"
    | "homeName"
    | "isOnline"
    | "lastSeenAt"
    | "name"
    | "roomId"
    | "roomName"
    | "uptime"
  >
>;

type CapabilityStateDelta = {
  reportedAt?: number | null;
  state?: unknown;
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function capabilityKey(endpointId: string, capabilityId: string, version: number) {
  return [
    endpointId.trim().toLowerCase(),
    capabilityId.trim().toLowerCase(),
    String(version),
  ].join("|");
}

function clearRuntimeStates(endpoints: DeviceEndpointDto[]) {
  return endpoints.map((endpoint) => ({
    ...endpoint,
    capabilities: endpoint.capabilities.map((capability) => ({
      ...capability,
      state: null,
    })),
  }));
}

function syncFlatCapabilities(
  device: DeviceDetailDto,
  endpoints: DeviceEndpointDto[]
): DeviceDetailDto["capabilities"] {
  const existingByKey = new Map(
    device.capabilities.map((capability) => [
      capabilityKey(
        capability.endpointId,
        capability.capabilityId,
        capability.capabilityVersion
      ),
      capability,
    ])
  );

  return endpoints.flatMap((endpoint) =>
    endpoint.capabilities
      .map((capability) => {
        const existing = existingByKey.get(
          capabilityKey(
            endpoint.endpointId,
            capability.capabilityId,
            capability.capabilityVersion
          )
        );

        if (!existing) {
          return null;
        }

        return {
          ...existing,
          supportedOperations: capability.supportedOperations,
          state: capability.state,
          lastReportedAt: capability.lastReportedAt,
        };
      })
      .filter((capability): capability is DeviceDetailDto["capabilities"][number] =>
        capability !== null
      )
  );
}

function withEndpoints(
  device: DeviceDetailDto,
  endpoints: DeviceEndpointDto[]
): DeviceDetailDto {
  return {
    ...device,
    endpoints,
    capabilities: syncFlatCapabilities(device, endpoints),
  };
}

function updateCapabilityRuntime(
  capability: DeviceEndpointCapabilityRuntimeDto,
  event: RealtimeDeltaEvent
): DeviceEndpointCapabilityRuntimeDto {
  if (
    !event.capabilityId ||
    capability.capabilityId.toLowerCase() !== event.capabilityId.toLowerCase()
  ) {
    return capability;
  }

  const delta = isRecord(event.delta) ? (event.delta as CapabilityStateDelta) : {};

  return {
    ...capability,
    state: Object.prototype.hasOwnProperty.call(delta, "state")
      ? delta.state
      : capability.state,
    lastReportedAt:
      typeof delta.reportedAt === "number"
        ? delta.reportedAt
        : capability.lastReportedAt,
  };
}

function applyCapabilityStateDelta(
  device: DeviceDetailDto,
  event: RealtimeDeltaEvent
): DeviceDetailDto {
  if (!event.endpointId || !event.capabilityId) {
    return device;
  }

  const normalizedEndpointId = event.endpointId.trim().toLowerCase();
  const endpoints = device.endpoints.map((endpoint) => {
    if (endpoint.endpointId.trim().toLowerCase() !== normalizedEndpointId) {
      return endpoint;
    }

    return {
      ...endpoint,
      capabilities: endpoint.capabilities.map((capability) =>
        updateCapabilityRuntime(capability, event)
      ),
    };
  });

  return withEndpoints(device, endpoints);
}

function applyDeviceDelta(
  device: DeviceDetailDto,
  event: RealtimeDeltaEvent
): DeviceDetailDto {
  const delta = isRecord(event.delta) ? (event.delta as DeviceDelta) : {};
  const nextDevice: DeviceDetailDto = {
    ...device,
    ...delta,
    homeId: event.homeId ?? delta.homeId ?? device.homeId,
    roomId: event.roomId ?? delta.roomId ?? device.roomId,
  };

  if (delta.isOnline === false) {
    return withEndpoints(nextDevice, clearRuntimeStates(nextDevice.endpoints));
  }

  return nextDevice;
}

export function applyDeviceRealtimeDelta(
  device: DeviceDetailDto | null,
  event: RealtimeDeltaEvent
): DeviceDetailDto | null {
  if (!device || event.deviceId !== device.id) {
    return device;
  }

  if (event.entity === "Device" && event.change === "Deleted") {
    return null;
  }

  if (event.entity === "Device") {
    return applyDeviceDelta(device, event);
  }

  if (event.entity === "DeviceCapability") {
    return applyCapabilityStateDelta(device, event);
  }

  return device;
}
