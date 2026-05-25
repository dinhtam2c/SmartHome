import { useCallback, useEffect, useState } from "react";
import { floorsApi } from "../api/floorsApi";
import type { FloorSummary } from "../types/floorTypes";

function sortFloors(floors: FloorSummary[]) {
  return [...floors].sort((left, right) => left.sortOrder - right.sortOrder);
}

export function useFloors(homeId: string | null) {
  const [floors, setFloors] = useState<FloorSummary[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const loadFloors = useCallback(
    async (silent = false) => {
      if (!homeId) {
        setFloors([]);
        setError(null);
        setIsLoading(false);
        return [];
      }

      if (!silent) {
        setIsLoading(true);
      }

      setError(null);

      try {
        const data = await floorsApi.list(homeId);
        const sortedFloors = sortFloors(data);
        setFloors(sortedFloors);
        return sortedFloors;
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

  useEffect(() => {
    void loadFloors();
  }, [loadFloors]);

  const reload = useCallback(
    (silent = false) => loadFloors(silent),
    [loadFloors]
  );

  return {
    floors,
    isLoading,
    error,
    reload,
    setFloors,
  };
}
