import { useCallback, useEffect, useState } from "react";
import { getDeviceDetail } from "../api/devicesApi";
import type { DeviceDetailDto } from "../types/deviceTypes";
import { applyDeviceRealtimeDelta } from "../services/deviceRealtimeService";
import { subscribeToRealtimeDeltas } from "@/shared/api/sse";

export function useDeviceDetail(deviceId: string | null) {
  const [device, setDevice] = useState<DeviceDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  const [commandEventVersion, setCommandEventVersion] = useState(0);

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

    const cleanup = subscribeToRealtimeDeltas({
      path: `/devices/${deviceId}/events`,
      onDelta: (event) => {
        if (event.deviceId && event.deviceId !== deviceId) {
          return;
        }

        if (event.entity === "DeviceCommandExecution") {
          setCommandEventVersion((current) => current + 1);
          return;
        }

        if (event.entity === "Device" || event.entity === "DeviceCapability") {
          setDevice((current) => applyDeviceRealtimeDelta(current, event));
          setError(null);
        }
      },
      onReconnect: () => {
        void loadDevice(true);
      },
    });

    return () => {
      cleanup();
    };
  }, [deviceId, loadDevice]);

  return {
    device,
    isLoading,
    error,
    commandEventVersion,
    reload: (silent = false) => loadDevice(silent),
  };
}
