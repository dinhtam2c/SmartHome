import { useCallback, useRef } from "react";
import type { FloorUpdatedReason } from "../types/floorTypes";

const LOCAL_EVENT_TTL_MS = 8_000;

type ExpectedLocalUpdate = {
  reason: FloorUpdatedReason;
  expiresAt: number;
};

export function useFloorLocalEvents() {
  const expectedUpdatesRef = useRef<ExpectedLocalUpdate[]>([]);

  const markExpectedLocalUpdate = useCallback((reason: FloorUpdatedReason) => {
    const now = Date.now();
    expectedUpdatesRef.current = [
      ...expectedUpdatesRef.current.filter((update) => update.expiresAt > now),
      { reason, expiresAt: now + LOCAL_EVENT_TTL_MS },
    ];
  }, []);

  const consumeExpectedLocalUpdate = useCallback((reason: FloorUpdatedReason) => {
    const now = Date.now();
    const pending = expectedUpdatesRef.current.filter(
      (update) => update.expiresAt > now
    );
    const matchingIndex = pending.findIndex((update) => update.reason === reason);
    if (matchingIndex === -1) {
      expectedUpdatesRef.current = pending;
      return false;
    }

    pending.splice(matchingIndex, 1);
    expectedUpdatesRef.current = pending;
    return true;
  }, []);

  return { consumeExpectedLocalUpdate, markExpectedLocalUpdate };
}
