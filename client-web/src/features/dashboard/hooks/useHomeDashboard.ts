import { useEffect, useState } from "react";
import type {
  DashboardHomeDto,
  DashboardHomeSummaryDto,
  DashboardLocationListItemDto,
} from "../dashboard.types";
import { getDashboardHome } from "../dashboard.api";

export function useHomeDashboard(homeId: string) {
  const [home, setHome] = useState<DashboardHomeDto | null>(null);
  const [summary, setSummary] = useState<DashboardHomeSummaryDto | null>(null);
  const [locations, setLocations] = useState<DashboardLocationListItemDto[]>(
    []
  );
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchHomeDashboard = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const homeData = await getDashboardHome(homeId);
      setHome(homeData);
      setSummary(homeData.summary);
      setLocations(homeData.locations);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchHomeDashboard();
  }, [homeId]);

  return { home, summary, locations, isLoading, error };
}
