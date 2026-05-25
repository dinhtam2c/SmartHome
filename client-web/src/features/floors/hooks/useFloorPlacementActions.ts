import { useCallback, useState } from "react";
import { useTranslation } from "react-i18next";
import { useToast } from "@/shared/ui/Toast";
import { floorsApi } from "../api/floorsApi";
import type { useFloorEditor } from "./useFloorEditor";
import type { Floor, FloorUpdatedReason, Point } from "../types/floorTypes";

type EditorState = ReturnType<typeof useFloorEditor>;
type Reload = (silent?: boolean) => Promise<unknown>;

type Params = {
  homeId: string | null;
  floor: Floor | null;
  editor: EditorState;
  markExpectedLocalUpdate: (reason: FloorUpdatedReason) => void;
  reloadFloor: Reload;
  reloadFloors: Reload;
  reloadDevices: Reload;
};

export function useFloorPlacementActions({
  homeId,
  floor,
  editor,
  markExpectedLocalUpdate,
  reloadFloor,
  reloadFloors,
  reloadDevices,
}: Params) {
  const { t } = useTranslation("floors");
  const { pushToast } = useToast();
  const [controlPanelDeviceId, setControlPanelDeviceId] = useState<string | null>(null);
  const [isRemovingPlacement, setIsRemovingPlacement] = useState(false);
  const closeControlPanel = useCallback(() => setControlPanelDeviceId(null), []);

  const selectDeviceForPlacement = useCallback((deviceId: string) => {
    editor.enterPlaceDeviceMode();
    editor.clearSelection();
    editor.setPendingPlacementDeviceId(deviceId);
  }, [editor]);

  const placeDevice = useCallback(async (deviceId: string, point: Point) => {
    if (!homeId || !floor) return;

    try {
      markExpectedLocalUpdate("DevicePlaced");
      const response = await floorsApi.placeDevice(homeId, floor.id, {
        deviceId,
        x: point.x,
        y: point.y,
      });
      await Promise.all([reloadFloor(true), reloadFloors(true), reloadDevices(true)]);
      editor.setPendingPlacementDeviceId(null);
      editor.selectPlacement(response.id);
    } catch (error) {
      pushToast({
        tone: "error",
        message: (error as Error).message || t("errors.placeDeviceFailed"),
      });
    }
  }, [
    editor,
    floor,
    homeId,
    markExpectedLocalUpdate,
    pushToast,
    reloadDevices,
    reloadFloor,
    reloadFloors,
    t,
  ]);

  const movePlacement = useCallback(async (placementId: string, point: Point) => {
    if (!homeId || !floor) return;

    try {
      markExpectedLocalUpdate("DeviceMoved");
      await floorsApi.moveDevice(homeId, floor.id, placementId, {
        x: point.x,
        y: point.y,
      });
      await Promise.all([reloadFloor(true), reloadDevices(true)]);
    } catch (error) {
      pushToast({
        tone: "error",
        message: (error as Error).message || t("errors.moveDeviceFailed"),
      });
      await reloadFloor(true);
    }
  }, [
    floor,
    homeId,
    markExpectedLocalUpdate,
    pushToast,
    reloadDevices,
    reloadFloor,
    t,
  ]);

  const removePlacement = useCallback(async (placementId: string) => {
    if (!homeId || !floor) return;
    if (!window.confirm(t("confirmRemovePlacement"))) return;

    setIsRemovingPlacement(true);
    try {
      markExpectedLocalUpdate("DeviceRemoved");
      await floorsApi.removeDevicePlacement(homeId, floor.id, placementId);
      editor.clearSelection();
      await Promise.all([reloadFloor(true), reloadFloors(true)]);
    } catch (error) {
      pushToast({
        tone: "error",
        message: (error as Error).message || t("errors.removePlacementFailed"),
      });
    } finally {
      setIsRemovingPlacement(false);
    }
  }, [
    editor,
    floor,
    homeId,
    markExpectedLocalUpdate,
    pushToast,
    reloadFloor,
    reloadFloors,
    setIsRemovingPlacement,
    t,
  ]);

  const handleDeviceClick = useCallback((placementId: string) => {
    const placement = floor?.devicePlacements.find((device) => device.id === placementId);
    if (!placement) return;

    if (editor.mode === "view") {
      setControlPanelDeviceId(placement.deviceId);
    } else if (editor.mode === "place-device") {
      editor.setPendingPlacementDeviceId(null);
      editor.selectPlacement(placementId);
    }
  }, [editor, floor?.devicePlacements, setControlPanelDeviceId]);

  const handleRoomClick = useCallback((roomId: string) => {
    if (editor.mode !== "place-device") return;
    editor.setPendingPlacementDeviceId(null);
    editor.selectRoom(roomId);
  }, [editor]);

  return {
    closeControlPanel,
    controlPanelDeviceId,
    handleDeviceClick,
    handleRoomClick,
    isRemovingPlacement,
    movePlacement,
    placeDevice,
    removePlacement,
    selectDeviceForPlacement,
  };
}
