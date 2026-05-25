import { useCallback, useEffect, useRef, useState } from "react";
import {
  subscribeToRealtimeDeltas,
  type RealtimeDeltaEvent,
} from "@/shared/api/sse";
import { normalizeFloorReason } from "../services/floorFormService";
import type { FloorUpdatedReason } from "../types/floorTypes";

type Reload = (silent?: boolean) => Promise<unknown>;

type Params = {
  homeId: string | null;
  floorId: string | null;
  editingSessionActive: boolean;
  controlPanelDeviceId: string | null;
  reloadHome: Reload;
  reloadFloors: Reload;
  reloadFloor: Reload;
  reloadDevices: Reload;
  applyDeviceRealtimeDelta: (event: RealtimeDeltaEvent) => void;
  closeControlPanel: () => void;
  consumeExpectedLocalUpdate: (reason: FloorUpdatedReason) => boolean;
};

export function useFloorRealtime({
  homeId,
  floorId,
  editingSessionActive,
  controlPanelDeviceId,
  reloadHome,
  reloadFloors,
  reloadFloor,
  reloadDevices,
  applyDeviceRealtimeDelta,
  closeControlPanel,
  consumeExpectedLocalUpdate,
}: Params) {
  const [pendingExternalUpdate, setPendingExternalUpdate] =
    useState<FloorUpdatedReason | null>(null);
  const editingSessionRef = useRef(editingSessionActive);
  const controlPanelDeviceIdRef = useRef(controlPanelDeviceId);

  useEffect(() => {
    editingSessionRef.current = editingSessionActive;
  }, [editingSessionActive]);

  useEffect(() => {
    controlPanelDeviceIdRef.current = controlPanelDeviceId;
  }, [controlPanelDeviceId]);

  const reloadAll = useCallback(
    async (silent = true) => {
      await Promise.all([
        reloadHome(silent),
        reloadFloors(silent),
        reloadFloor(silent),
        reloadDevices(silent),
      ]);
    },
    [reloadDevices, reloadFloor, reloadFloors, reloadHome]
  );

  useEffect(() => {
    if (!homeId) return undefined;

    return subscribeToRealtimeDeltas({
      path: `/homes/${homeId}/events`,
      onDelta: (event) => {
        if (event.homeId && event.homeId !== homeId) return;

        if (event.entity === "Device" || event.entity === "DeviceCapability") {
          if (event.entity === "Device" && event.change === "Created") {
            void reloadDevices(true);
            return;
          }

          applyDeviceRealtimeDelta(event);
          if (event.entity === "Device" && event.change === "Moved") {
            void reloadDevices(true);
          }
          if (
            event.entity === "Device"
            && event.change === "Deleted"
            && event.deviceId
            && controlPanelDeviceIdRef.current === event.deviceId
          ) {
            closeControlPanel();
          }
          return;
        }

        if (event.entity === "Room") {
          void Promise.all([reloadHome(true), reloadDevices(true)]);
          return;
        }

        if (event.entity !== "Floor") return;

        const delta = event.delta
          && typeof event.delta === "object"
          && !Array.isArray(event.delta)
          ? event.delta as Record<string, unknown>
          : {};
        const reason = normalizeFloorReason(
          typeof delta.reason === "string" ? delta.reason : null
        );
        const listChanged = delta.listChanged === true;
        const changedFloorId = event.floorId?.trim();

        if (changedFloorId && changedFloorId !== floorId) {
          if (listChanged) void reloadFloors(true);
          return;
        }
        if (consumeExpectedLocalUpdate(reason)) return;

        if (listChanged || event.change === "Created" || event.change === "Deleted") {
          void reloadFloors(true);
        }
        if (editingSessionRef.current) {
          setPendingExternalUpdate((current) => current ?? reason);
          return;
        }
        void reloadFloor(true);
      },
      onReconnect: () => {
        if (!editingSessionRef.current) void reloadAll(true);
      },
    });
  }, [
    applyDeviceRealtimeDelta,
    closeControlPanel,
    consumeExpectedLocalUpdate,
    floorId,
    homeId,
    reloadAll,
    reloadDevices,
    reloadFloor,
    reloadFloors,
    reloadHome,
  ]);

  return {
    pendingExternalUpdate,
    reloadAll,
    setPendingExternalUpdate,
  };
}
