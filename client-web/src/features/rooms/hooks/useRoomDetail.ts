import { useCallback, useEffect, useState } from "react";
import { getHomeDetail } from "@/features/homes";
import type { HomeDetailDto } from "@/features/homes";
import { getRoomDetail } from "../api/roomsApi";
import type { RoomDetailDto } from "../types/roomTypes";
import type { RealtimeDeltaEvent } from "@/shared/api/sse";
import { subscribeToRealtimeDeltas } from "@/shared/api/sse";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function applyDeviceDelta(room: RoomDetailDto, event: RealtimeDeltaEvent) {
  if (!event.deviceId) {
    return room;
  }

  if (event.change === "Deleted" || event.roomId !== room.id) {
    const removed = room.devices.find((device) => device.id === event.deviceId);
    if (!removed) {
      return room;
    }

    return {
      ...room,
      deviceCount: Math.max(0, room.deviceCount - 1),
      onlineDeviceCount: removed.isOnline
        ? Math.max(0, room.onlineDeviceCount - 1)
        : room.onlineDeviceCount,
      devices: room.devices.filter((device) => device.id !== event.deviceId),
    };
  }

  if (!isRecord(event.delta)) {
    return room;
  }

  const delta = event.delta;
  let onlineDelta = 0;
  const devices = room.devices.map((device) => {
    if (device.id !== event.deviceId) {
      return device;
    }

    const nextIsOnline =
      typeof delta.isOnline === "boolean"
        ? delta.isOnline
        : device.isOnline;

    if (nextIsOnline !== device.isOnline) {
      onlineDelta = nextIsOnline ? 1 : -1;
    }

    return {
      ...device,
      name: typeof delta.name === "string" ? delta.name : device.name,
      isOnline: nextIsOnline,
    };
  });

  return {
    ...room,
    onlineDeviceCount: Math.max(0, room.onlineDeviceCount + onlineDelta),
    devices,
  };
}

function applyCapabilityDelta(room: RoomDetailDto, event: RealtimeDeltaEvent) {
  if (!event.deviceId || !event.endpointId || !event.capabilityId || !isRecord(event.delta)) {
    return room;
  }

  const delta = event.delta;
  const endpointId = event.endpointId.trim().toLowerCase();
  const capabilityId = event.capabilityId.trim().toLowerCase();
  const reportedAt =
    typeof delta.reportedAt === "number" ? delta.reportedAt : null;
  const hasState = Object.prototype.hasOwnProperty.call(delta, "state");
  const state = isRecord(delta.state) ? delta.state : null;
  const aggregateValue =
    state && typeof state.value === "number" && Number.isFinite(state.value)
      ? state.value
      : null;
  const aggregateKey =
    event.capabilityId === "sensor.temperature"
      ? "temperature"
      : event.capabilityId === "sensor.humidity"
        ? "humidity"
        : null;

  return {
    ...room,
    ...(aggregateKey && aggregateValue !== null
      ? { [aggregateKey]: aggregateValue }
      : {}),
    devices: room.devices.map((device) => {
      if (device.id !== event.deviceId) {
        return device;
      }

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
                state: hasState ? delta.state : capability.state,
                lastReportedAt: reportedAt ?? capability.lastReportedAt,
              };
            }),
          };
        }),
      };
    }),
  };
}

function applyRoomRealtimeDelta(
  room: RoomDetailDto | null,
  event: RealtimeDeltaEvent
): RoomDetailDto | null {
  if (!room) {
    return room;
  }

  if (event.entity === "Room") {
    if (event.change === "Deleted" && (!event.roomId || event.roomId === room.id)) {
      return null;
    }

    if (event.roomId === room.id && isRecord(event.delta)) {
      return {
        ...room,
        name: typeof event.delta.name === "string" ? event.delta.name : room.name,
        description:
          typeof event.delta.description === "string" ||
            event.delta.description === null
            ? event.delta.description
            : room.description,
      };
    }
  }

  if (event.entity === "Device") {
    return applyDeviceDelta(room, event);
  }

  if (event.entity === "DeviceCapability") {
    return applyCapabilityDelta(room, event);
  }

  return room;
}

export function useRoomDetail(homeId: string | null, roomId: string | null) {
  const [room, setRoom] = useState<RoomDetailDto | null>(null);
  const [home, setHome] = useState<HomeDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const loadRoom = useCallback(async (silent = false) => {
    if (!homeId || !roomId) return;

    if (!silent) {
      setIsLoading(true);
    }

    setError(null);

    try {
      const [homeDetail, roomDetail] = await Promise.all([
        getHomeDetail(homeId),
        getRoomDetail(homeId, roomId),
      ]);

      setRoom(roomDetail);
      setHome(homeDetail);
    } catch (error) {
      setError(error as Error);
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, [homeId, roomId]);

  useEffect(() => {
    void loadRoom();
  }, [loadRoom]);

  useEffect(() => {
    if (!homeId || !roomId) return;

    const cleanup = subscribeToRealtimeDeltas({
      path: `/homes/${homeId}/rooms/${roomId}/events`,
      onDelta: (event) => {
        if (event.roomId && event.roomId !== roomId && event.previousRoomId !== roomId) {
          return;
        }

        if (
          event.entity === "Device" &&
          event.change === "Moved" &&
          event.roomId === roomId
        ) {
          void loadRoom(true);
          return;
        }

        setRoom((current) => applyRoomRealtimeDelta(current, event));
        setError(null);
      },
      onReconnect: () => {
        void loadRoom(true);
      },
    });

    return () => {
      cleanup();
    };
  }, [homeId, loadRoom, roomId]);

  return { room, home, isLoading, error, reload: (silent = false) => loadRoom(silent) };
}
