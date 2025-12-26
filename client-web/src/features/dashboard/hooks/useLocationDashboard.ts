import { useEffect, useState } from "react";
import type {
  DashboardLocationDto,
  DashboardLocationSummaryDto,
  DashboardDeviceListItemDto,
} from "../dashboard.types";
import { getDashboardLocation } from "../dashboard.api";

export function useLocationDashboard(locationId: string) {
  const [location, setLocation] = useState<DashboardLocationDto | null>(null);
  const [summary, setSummary] = useState<DashboardLocationSummaryDto | null>(
    null
  );
  const [devices, setDevices] = useState<DashboardDeviceListItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchLocationDashboard = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const locationData = await getDashboardLocation(locationId);
      setLocation(locationData);
      setSummary(locationData.summary);
      setDevices(locationData.devices);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchLocationDashboard();
  }, [locationId]);

  return { location, summary, devices, isLoading, error };
}
