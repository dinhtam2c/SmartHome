import { useCallback, useEffect, useState } from "react";
import { getDeviceDetail } from "../devices.api";
import type { DeviceDetailDto } from "../devices.types";
import { parseSseEventData, subscribeToSse } from "@/services/sse";

type DeviceEventPayload = {
  DeviceId?: string;
};

export function useDeviceDetail(deviceId: string | null) {
  const [device, setDevice] = useState<DeviceDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const loadDevice = useCallback(async (silent = false) => {
    if (!deviceId) return;

    if (!silent) {
      setIsLoading(true);
    }

    setError(null);

    try {
      const data = await getDeviceDetail(deviceId);
      setDevice(data);
    } catch (error) {
      setError(error as Error);
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, [deviceId]);

  useEffect(() => {
    void loadDevice();
  }, [loadDevice]);

  useEffect(() => {
    if (!deviceId) return;

    const cleanup = subscribeToSse({
      path: `/devices/${deviceId}/events`,
      handlers: {
        DeviceDetailsChanged: (event) => {
          const payload = parseSseEventData<DeviceEventPayload>(event);

          if (!payload?.DeviceId || payload.DeviceId === deviceId) {
            void loadDevice(true);
          }
        },
        DeviceDeleted: (event) => {
          const payload = parseSseEventData<DeviceEventPayload>(event);

          if (!payload?.DeviceId || payload.DeviceId === deviceId) {
            setDevice(null);
            setError(null);
          }
        },
      },
      onReconnect: () => {
        void loadDevice(true);
      },
    });

    return () => {
      cleanup();
    };
  }, [deviceId, loadDevice]);

  return { device, isLoading, error, reload: (silent = false) => loadDevice(silent) };
}
