import { useCallback, useEffect, useMemo, useState } from "react";
import { getHomeDevices } from "@/features/homes";
import type { SelectableDeviceDto } from "@/features/capabilities";
import type { RealtimeDeltaEvent } from "@/shared/api/sse";

function sortDevices(devices: SelectableDeviceDto[]) {
  return [...devices].sort((left, right) =>
    left.name.localeCompare(right.name, undefined, { sensitivity: "base" })
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function patchDevice(device: SelectableDeviceDto, event: RealtimeDeltaEvent) {
  if (!isRecord(event.delta)) {
    return {
      ...device,
      roomId: event.roomId ?? device.roomId,
    };
  }

  const hasRoomId = Object.prototype.hasOwnProperty.call(event.delta, "roomId");
  const nextRoomId = hasRoomId
    ? typeof event.delta.roomId === "string"
      ? event.delta.roomId
      : null
    : device.roomId;

  return {
    ...device,
    name: typeof event.delta.name === "string" ? event.delta.name : device.name,
    isOnline:
      typeof event.delta.isOnline === "boolean"
        ? event.delta.isOnline
        : device.isOnline,
    lastSeenAt:
      typeof event.delta.lastSeenAt === "number"
        ? event.delta.lastSeenAt
        : device.lastSeenAt,
    uptime:
      typeof event.delta.uptime === "number" ? event.delta.uptime : device.uptime,
    roomId: nextRoomId,
  };
}

function patchCapability(device: SelectableDeviceDto, event: RealtimeDeltaEvent) {
  if (!event.endpointId || !event.capabilityId || !isRecord(event.delta)) {
    return device;
  }

  const delta = event.delta;
  const endpointId = event.endpointId.trim().toLowerCase();
  const capabilityId = event.capabilityId.trim().toLowerCase();
  const reportedAt =
    typeof delta.reportedAt === "number" ? delta.reportedAt : null;
  const hasState = Object.prototype.hasOwnProperty.call(delta, "state");

  return {
    ...device,
    endpoints: device.endpoints.map((endpoint) => {
      if (endpoint.endpointId.trim().toLowerCase() !== endpointId) {
        return endpoint;
      }

      return {
        ...endpoint,
        capabilities: endpoint.capabilities.map((capability) => {
          if (capability.capabilityId.trim().toLowerCase() !== capabilityId) {
            return capability;
          }

          return {
            ...capability,
            state: hasState
              ? (delta.state as Record<string, unknown> | null)
              : capability.state,
            lastReportedAt: reportedAt ?? capability.lastReportedAt,
          };
        }),
      };
    }),
  };
}

export function useFloorDevices(homeId: string | null) {
  const [devices, setDevices] = useState<SelectableDeviceDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const loadDevices = useCallback(
    async (silent = false) => {
      if (!homeId) {
        setDevices([]);
        setError(null);
        setIsLoading(false);
        return [];
      }

      if (!silent) {
        setIsLoading(true);
      }

      setError(null);

      try {
        const nextDevices = await getHomeDevices(homeId);
        const sortedDevices = sortDevices(nextDevices);
        setDevices(sortedDevices);
        return sortedDevices;
      } catch (nextError) {
        setError(nextError as Error);
        return [];
      } finally {
        if (!silent) {
          setIsLoading(false);
        }
      }
    },
    [homeId]
  );

  const applyDeviceRealtimeDelta = useCallback((event: RealtimeDeltaEvent) => {
    setDevices((current) => {
      if (!event.deviceId) {
        return current;
      }

      if (event.entity === "Device" && event.change === "Deleted") {
        return current.filter((device) => device.id !== event.deviceId);
      }

      return sortDevices(
        current.map((device) => {
          if (device.id !== event.deviceId) {
            return device;
          }

          if (event.entity === "Device") {
            return patchDevice(device, event);
          }

          if (event.entity === "DeviceCapability") {
            return patchCapability(device, event);
          }

          return device;
        })
      );
    });
  }, []);

  const removeDevice = useCallback((deviceId: string | null | undefined) => {
    const normalizedDeviceId = deviceId?.trim();

    if (!normalizedDeviceId) {
      return;
    }

    setDevices((current) => current.filter((device) => device.id !== normalizedDeviceId));
  }, []);

  useEffect(() => {
    void loadDevices();
  }, [loadDevices]);

  const devicesById = useMemo(
    () => new Map(devices.map((device) => [device.id, device])),
    [devices]
  );

  const reload = useCallback(
    (silent = false) => loadDevices(silent),
    [loadDevices]
  );

  return {
    devices,
    devicesById,
    isLoading,
    error,
    reload,
    removeDevice,
    applyDeviceRealtimeDelta,
  };
}
