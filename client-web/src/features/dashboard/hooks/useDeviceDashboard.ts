import { useEffect, useState } from "react";
import type { DashboardDeviceDto } from "../dashboard.types";
import { getDashboardDevice } from "../dashboard.api";

export function useDeviceDashboard(deviceId: string | null) {
  const [device, setDevice] = useState<DashboardDeviceDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchDeviceDashboard = async (id: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const deviceData = await getDashboardDevice(id);
      setDevice(deviceData);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (deviceId) {
      fetchDeviceDashboard(deviceId);
    } else {
      setDevice(null);
      setError(null);
    }
  }, [deviceId]);

  return {
    device,
    isLoading,
    error,
    refetch: () => deviceId && fetchDeviceDashboard(deviceId),
  };
}
