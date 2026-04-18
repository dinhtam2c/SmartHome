import { useCallback, useEffect, useState } from "react";
import { getHomeDetail } from "@/features/homes/homes.api";
import type { HomeDetailDto } from "@/features/homes/homes.types";
import { getRoomDetail } from "../rooms.api";
import type { RoomDetailDto } from "../rooms.types";
import { parseSseEventData, subscribeToSse } from "@/services/sse";

type RoomDeletedEventPayload = {
  RoomId?: string;
};

export function useRoomDetail(homeId: string | null, roomId: string | null) {
  const [room, setRoom] = useState<RoomDetailDto | null>(null);
  const [home, setHome] = useState<HomeDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const loadRoom = useCallback(async (silent = false) => {
    if (!homeId || !roomId) return;

    if (!silent) {
      setIsLoading(true);
    }

    setError(null);

    try {
      const [homeDetail, roomDetail] = await Promise.all([
        getHomeDetail(homeId),
        getRoomDetail(homeId, roomId),
      ]);

      setRoom(roomDetail);
      setHome(homeDetail);
    } catch (error) {
      setError(error as Error);
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, [homeId, roomId]);

  useEffect(() => {
    void loadRoom();
  }, [loadRoom]);

  useEffect(() => {
    if (!homeId || !roomId) return;

    const cleanup = subscribeToSse({
      path: `/homes/${homeId}/rooms/${roomId}/events`,
      handlers: {
        DeviceDetailsChanged: () => {
          void loadRoom(true);
        },
        DeviceDeleted: () => {
          void loadRoom(true);
        },
        RoomDetailsChanged: () => {
          void loadRoom(true);
        },
        RoomDeleted: (event) => {
          const payload = parseSseEventData<RoomDeletedEventPayload>(event);

          if (!payload?.RoomId || payload.RoomId === roomId) {
            setRoom(null);
            setError(null);
          }
        },
      },
      onReconnect: () => {
        void loadRoom(true);
      },
    });

    return () => {
      cleanup();
    };
  }, [homeId, loadRoom, roomId]);

  return { room, home, isLoading, error, reload: (silent = false) => loadRoom(silent) };
}
