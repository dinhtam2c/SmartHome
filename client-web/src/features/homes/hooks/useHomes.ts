import { useCallback, useEffect, useState } from "react";
import { getHomes } from "../api/homesApi";
import type { HomeListItemDto } from "../types/homeTypes";

export function useHomes() {
  const [homes, setHomes] = useState<HomeListItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const loadHomes = useCallback(async (silent = false) => {
    if (!silent) {
      setIsLoading(true);
    }

    setError(null);

    try {
      const homeItems = await getHomes();
      setHomes(homeItems);
    } catch (error) {
      setError(error as Error);
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    void loadHomes();
  }, [loadHomes]);

  return { homes, isLoading, error, reload: (silent = false) => loadHomes(silent) };
}
