import { useEffect, useState } from "react";
import type { DashboardHomeListItemDto } from "../dashboard.types";
import { getHomes as apiGetHomes } from "../dashboard.api";

export function useHomes() {
  const [homes, setHomes] = useState<DashboardHomeListItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  async function getHomes() {
    try {
      setIsLoading(true);
      setError(null);
      const homes = await apiGetHomes();
      setHomes(homes);
    } catch (error) {
      setError(error as Error);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    getHomes();
  }, []);

  return { homes, isLoading, error };
}
