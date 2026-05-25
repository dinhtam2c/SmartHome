import { useCallback, useEffect, useState } from "react";
import { floorsApi } from "../api/floorsApi";
import type { Floor } from "../types/floorTypes";

export function useFloor(homeId: string | null, floorId: string | null) {
  const [floor, setFloor] = useState<Floor | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const loadFloor = useCallback(
    async (silent = false) => {
      if (!homeId || !floorId) {
        setFloor(null);
        setError(null);
        setIsLoading(false);
        return null;
      }

      if (!silent) {
        setFloor(null);
        setIsLoading(true);
      }

      setError(null);

      try {
        const data = await floorsApi.get(homeId, floorId);
        setFloor(data);
        return data;
      } catch (nextError) {
        setError(nextError as Error);
        return null;
      } finally {
        if (!silent) {
          setIsLoading(false);
        }
      }
    },
    [floorId, homeId]
  );

  useEffect(() => {
    void loadFloor();
  }, [loadFloor]);

  const reload = useCallback(
    (silent = false) => loadFloor(silent),
    [loadFloor]
  );

  return {
    floor,
    isLoading,
    error,
    reload,
    setFloor,
  };
}
