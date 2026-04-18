import { useCallback, useEffect, useState } from "react";
import { getHomeDetail } from "../homes.api";
import type { HomeDetailDto } from "../homes.types";
import { subscribeToSse } from "@/services/sse";

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

    const cleanup = subscribeToSse({
      path: `/homes/${homeId}/events`,
      handlers: {
        DeviceDetailsChanged: () => {
          void loadHome(true);
        },
        DeviceDeleted: () => {
          void loadHome(true);
        },
        RoomDetailsChanged: () => {
          void loadHome(true);
        },
        RoomDeleted: () => {
          void loadHome(true);
        },
        SceneDetailsChanged: () => {
          void loadHome(true);
        },
        SceneDeleted: () => {
          void loadHome(true);
        },
      },
      onReconnect: () => {
        void loadHome(true);
      },
    });

    return () => {
      cleanup();
    };
  }, [homeId, loadHome]);

  return { home, isLoading, error, reload: (silent = false) => loadHome(silent) };
}
