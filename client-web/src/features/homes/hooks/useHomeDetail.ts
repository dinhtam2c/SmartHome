import { useCallback, useEffect, useState } from "react";
import { getHomeDetail } from "../api/homesApi";
import type {
  HomeDetailDto,
  HomeRoomOverviewDto,
  HomeSceneSummaryDto,
  HomeUnassignedDeviceOverviewDto,
} from "../types/homeTypes";
import type { RealtimeDeltaEvent } from "@/shared/api/sse";
import { subscribeToRealtimeDeltas } from "@/shared/api/sse";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function toNumber(value: unknown) {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function patchRoom(
  room: HomeRoomOverviewDto,
  delta: Record<string, unknown>
): HomeRoomOverviewDto {
  return {
    ...room,
    name: typeof delta.name === "string" ? delta.name : room.name,
    description:
      typeof delta.description === "string" || delta.description === null
        ? delta.description
        : room.description,
  };
}

function buildRoom(event: RealtimeDeltaEvent): HomeRoomOverviewDto | null {
  if (!event.roomId || !isRecord(event.delta)) {
    return null;
  }

  return {
    id: event.roomId,
    name: typeof event.delta.name === "string" ? event.delta.name : "",
    description:
      typeof event.delta.description === "string" ? event.delta.description : null,
    deviceCount: toNumber(event.delta.deviceCount) ?? 0,
    onlineDeviceCount: toNumber(event.delta.onlineDeviceCount) ?? 0,
    temperature: toNumber(event.delta.temperature),
    humidity: toNumber(event.delta.humidity),
  };
}

function applyRoomDelta(home: HomeDetailDto, event: RealtimeDeltaEvent) {
  if (!event.roomId) {
    return home;
  }

  if (event.change === "Deleted") {
    const hadRoom = home.rooms.some((room) => room.id === event.roomId);
    return {
      ...home,
      rooms: home.rooms.filter((room) => room.id !== event.roomId),
      roomCount: hadRoom ? Math.max(0, home.roomCount - 1) : home.roomCount,
    };
  }

  if (event.change === "Created") {
    const nextRoom = buildRoom(event);
    if (!nextRoom) {
      return home;
    }

    const exists = home.rooms.some((room) => room.id === nextRoom.id);
    return {
      ...home,
      rooms: exists
        ? home.rooms.map((room) => room.id === nextRoom.id ? nextRoom : room)
        : [...home.rooms, nextRoom],
      roomCount: exists ? home.roomCount : home.roomCount + 1,
    };
  }

  if (!isRecord(event.delta)) {
    return home;
  }

  return {
    ...home,
    rooms: home.rooms.map((room) =>
      room.id === event.roomId ? patchRoom(room, event.delta as Record<string, unknown>) : room
    ),
  };
}

function patchScene(
  scene: HomeSceneSummaryDto,
  delta: Record<string, unknown>
): HomeSceneSummaryDto {
  return {
    ...scene,
    name: typeof delta.name === "string" ? delta.name : scene.name,
    isEnabled:
      typeof delta.isEnabled === "boolean" ? delta.isEnabled : scene.isEnabled,
  };
}

function buildScene(event: RealtimeDeltaEvent): HomeSceneSummaryDto | null {
  if (!event.sceneId || !isRecord(event.delta)) {
    return null;
  }

  return {
    id: event.sceneId,
    name: typeof event.delta.name === "string" ? event.delta.name : "",
    isEnabled:
      typeof event.delta.isEnabled === "boolean" ? event.delta.isEnabled : true,
  };
}

function applySceneDelta(home: HomeDetailDto, event: RealtimeDeltaEvent) {
  if (!event.sceneId) {
    return home;
  }

  if (event.change === "Deleted") {
    return {
      ...home,
      scenes: home.scenes.filter((scene) => scene.id !== event.sceneId),
    };
  }

  const nextScene = buildScene(event);
  if (!nextScene) {
    return home;
  }

  const exists = home.scenes.some((scene) => scene.id === nextScene.id);
  return {
    ...home,
    scenes: exists
      ? home.scenes.map((scene) =>
        scene.id === nextScene.id
          ? patchScene(scene, event.delta as Record<string, unknown>)
          : scene
      )
      : [...home.scenes, nextScene],
  };
}

function patchUnassignedDevice(
  device: HomeUnassignedDeviceOverviewDto,
  event: RealtimeDeltaEvent
): HomeUnassignedDeviceOverviewDto {
  if (!isRecord(event.delta)) {
    return device;
  }

  return {
    ...device,
    name: typeof event.delta.name === "string" ? event.delta.name : device.name,
    isOnline:
      typeof event.delta.isOnline === "boolean"
        ? event.delta.isOnline
        : device.isOnline,
  };
}

function patchRoomCounts(
  rooms: HomeRoomOverviewDto[],
  roomId: string | null | undefined,
  deviceDelta: number,
  onlineDelta: number
) {
  if (!roomId) {
    return rooms;
  }

  return rooms.map((room) =>
    room.id === roomId
      ? {
        ...room,
        deviceCount: Math.max(0, room.deviceCount + deviceDelta),
        onlineDeviceCount: Math.max(0, room.onlineDeviceCount + onlineDelta),
      }
      : room
  );
}

function applyDeviceDelta(home: HomeDetailDto, event: RealtimeDeltaEvent) {
  if (!event.deviceId) {
    return home;
  }

  if (event.change === "Deleted") {
    const deleted = home.unassignedDevices.find(
      (device) => device.id === event.deviceId
    );
    const isOnline = isRecord(event.delta) && typeof event.delta.isOnline === "boolean"
      ? event.delta.isOnline
      : deleted?.isOnline;
    const onlineDelta = isOnline ? -1 : 0;

    return {
      ...home,
      deviceCount: Math.max(0, home.deviceCount - 1),
      onlineDeviceCount: Math.max(0, home.onlineDeviceCount + onlineDelta),
      rooms: patchRoomCounts(home.rooms, event.roomId, -1, onlineDelta),
      unassignedDevices: home.unassignedDevices.filter(
        (device) => device.id !== event.deviceId
      ),
    };
  }

  if (event.change === "StatusChanged" && isRecord(event.delta)) {
    const isOnline = event.delta.isOnline;
    const current = home.unassignedDevices.find(
      (device) => device.id === event.deviceId
    );
    const onlineDelta =
      typeof isOnline === "boolean" && current && current.isOnline !== isOnline
        ? isOnline
          ? 1
          : -1
        : 0;
    const roomOnlineDelta =
      typeof isOnline === "boolean" && event.roomId
        ? isOnline
          ? 1
          : -1
        : 0;

    return {
      ...home,
      onlineDeviceCount: Math.max(
        0,
        home.onlineDeviceCount + (current ? onlineDelta : roomOnlineDelta)
      ),
      rooms: patchRoomCounts(home.rooms, event.roomId, 0, roomOnlineDelta),
      unassignedDevices: home.unassignedDevices.map((device) =>
        device.id === event.deviceId ? patchUnassignedDevice(device, event) : device
      ),
    };
  }

  if (event.change === "Updated") {
    return {
      ...home,
      unassignedDevices: home.unassignedDevices.map((device) =>
        device.id === event.deviceId ? patchUnassignedDevice(device, event) : device
      ),
    };
  }

  return home;
}

function applyCapabilityDelta(home: HomeDetailDto, event: RealtimeDeltaEvent) {
  if (!event.capabilityId || !isRecord(event.delta)) {
    return home;
  }

  const delta = event.delta;
  const state = isRecord(delta.state) ? delta.state : null;
  const value = state ? toNumber(state.value) : null;
  const aggregateKey =
    event.capabilityId === "sensor.temperature"
      ? "temperature"
      : event.capabilityId === "sensor.humidity"
        ? "humidity"
        : null;

  return {
    ...home,
    unassignedDevices: home.unassignedDevices.map((device) => {
      if (!event.deviceId || device.id !== event.deviceId || !event.endpointId) {
        return device;
      }

      const endpointId = event.endpointId.trim().toLowerCase();
      const capabilityId = event.capabilityId?.trim().toLowerCase();
      const reportedAt =
        typeof delta.reportedAt === "number"
          ? delta.reportedAt
          : null;
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
                state: hasState ? delta.state : capability.state,
                lastReportedAt: reportedAt ?? capability.lastReportedAt,
              };
            }),
          };
        }),
      };
    }),
    rooms: home.rooms.map((room) =>
      aggregateKey && value !== null && room.id === event.roomId
        ? {
          ...room,
          [aggregateKey]: value,
        }
        : room
    ),
  };
}

function applyHomeRealtimeDelta(
  home: HomeDetailDto | null,
  event: RealtimeDeltaEvent
): HomeDetailDto | null {
  if (!home) {
    return home;
  }

  if (event.entity === "Room") {
    return applyRoomDelta(home, event);
  }

  if (event.entity === "Scene") {
    return applySceneDelta(home, event);
  }

  if (event.entity === "Device") {
    return applyDeviceDelta(home, event);
  }

  if (event.entity === "DeviceCapability") {
    return applyCapabilityDelta(home, event);
  }

  if (event.entity === "Floor") {
    if (event.change === "Created") {
      return { ...home, floorCount: home.floorCount + 1 };
    }

    if (event.change === "Deleted") {
      return { ...home, floorCount: Math.max(0, home.floorCount - 1) };
    }
  }

  return home;
}

export function useHomeDetail(homeId: string | null) {
  const [home, setHome] = useState<HomeDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const loadHome = useCallback(async (silent = false) => {
    if (!homeId) return;

    if (!silent) {
      setIsLoading(true);
    }

    setError(null);

    try {
      const homeDetail = await getHomeDetail(homeId);
      setHome(homeDetail);
    } catch (error) {
      setError(error as Error);
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, [homeId]);

  useEffect(() => {
    void loadHome();
  }, [loadHome]);

  useEffect(() => {
    if (!homeId) return;

    const cleanup = subscribeToRealtimeDeltas({
      path: `/homes/${homeId}/events`,
      onDelta: (event) => {
        if (event.homeId && event.homeId !== homeId) {
          return;
        }

        if (event.entity === "Device" && event.change === "Created") {
          void loadHome(true);
          return;
        }

        if (event.entity === "Device" && event.change === "Moved") {
          void loadHome(true);
          return;
        }

        setHome((current) => applyHomeRealtimeDelta(current, event));
      },
      onReconnect: () => {
        void loadHome(true);
      },
    });

    return () => {
      cleanup();
    };
  }, [homeId, loadHome]);

  const reload = useCallback(
    (silent = false) => loadHome(silent),
    [loadHome]
  );

  return { home, isLoading, error, reload };
}
