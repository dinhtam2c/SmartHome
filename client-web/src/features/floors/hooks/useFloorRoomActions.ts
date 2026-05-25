import { useCallback, useState, type SyntheticEvent } from "react";
import { useTranslation } from "react-i18next";
import type { HomeRoomOverviewDto } from "@/features/homes";
import { floorsApi } from "../api/floorsApi";
import { DEFAULT_ROOM_COLOR } from "../services/floorConstants";
import { createEmptyRoomDraft } from "../services/floorFormService";
import type { useFloorEditor } from "./useFloorEditor";
import type {
  CanvasRoom,
  Floor,
  FloorUpdatedReason,
  Point,
  RoomFormDraft,
} from "../types/floorTypes";

type EditorState = ReturnType<typeof useFloorEditor>;
type Reload = (silent?: boolean) => Promise<unknown>;

type Params = {
  homeId: string | null;
  floor: Floor | null;
  availableRooms: HomeRoomOverviewDto[];
  canvasRooms: CanvasRoom[];
  editor: EditorState;
  markExpectedLocalUpdate: (reason: FloorUpdatedReason) => void;
  reloadFloor: Reload;
  reloadFloors: Reload;
};

export function useFloorRoomActions({
  homeId,
  floor,
  availableRooms,
  canvasRooms,
  editor,
  markExpectedLocalUpdate,
  reloadFloor,
  reloadFloors,
}: Params) {
  const { t } = useTranslation("floors");
  const [isRoomModalOpen, setIsRoomModalOpen] = useState(false);
  const [roomModalMode, setRoomModalMode] = useState<"create" | "edit">("create");
  const [roomDraft, setRoomDraft] = useState<RoomFormDraft>(createEmptyRoomDraft([]));
  const [roomError, setRoomError] = useState<string | null>(null);
  const [isSavingRoom, setIsSavingRoom] = useState(false);
  const [isDeletingRoom, setIsDeletingRoom] = useState(false);

  const closeRoomModal = useCallback(() => {
    setIsRoomModalOpen(false);
    setRoomError(null);
  }, []);

  const createRoom = useCallback((polygon: Point[]) => {
    if (availableRooms.length === 0) {
      editor.enterPlaceDeviceMode();
      return;
    }

    setRoomModalMode("create");
    setRoomDraft(createEmptyRoomDraft(polygon, availableRooms[0].id));
    setRoomError(null);
    setIsRoomModalOpen(true);
  }, [
    availableRooms,
    editor,
    setIsRoomModalOpen,
    setRoomDraft,
    setRoomError,
    setRoomModalMode,
  ]);

  const finishDrawingRoom = useCallback(() => {
    const polygon = editor.finishDrawing();
    if (polygon) createRoom(polygon);
  }, [createRoom, editor]);

  const cancelDrawingRoom = useCallback(() => {
    editor.cancelDrawing("place-device");
  }, [editor]);

  const editRoom = useCallback((roomId: string) => {
    const room = canvasRooms.find((entry) => entry.id === roomId);
    if (!room) return;

    editor.selectRoom(roomId);
    setRoomModalMode("edit");
    setRoomDraft({
      floorPlanRoomId: room.id,
      roomId: room.roomId,
      fillColor: room.fillColor ?? DEFAULT_ROOM_COLOR,
      polygon: room.polygon,
    });
    setRoomError(null);
    setIsRoomModalOpen(true);
  }, [
    canvasRooms,
    editor,
    setIsRoomModalOpen,
    setRoomDraft,
    setRoomError,
    setRoomModalMode,
  ]);

  const saveRoom = useCallback(async (event: SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!homeId || !floor) return;

    if (!roomDraft.roomId) {
      setRoomError(t("errors.roomRequired", { defaultValue: "Vui lòng chọn phòng." }));
      return;
    }

    if (roomDraft.polygon.length < 3) {
      setRoomError(t("errors.roomPolygonRequired"));
      return;
    }

    setIsSavingRoom(true);
    setRoomError(null);

    try {
      let savedRoomId = roomDraft.floorPlanRoomId;
      if (roomModalMode === "edit" && roomDraft.floorPlanRoomId) {
        markExpectedLocalUpdate("RoomUpdated");
        await floorsApi.updateRoom(homeId, floor.id, roomDraft.floorPlanRoomId, {
          fillColor: roomDraft.fillColor || null,
          polygon: roomDraft.polygon,
        });
      } else {
        markExpectedLocalUpdate("RoomAdded");
        const response = await floorsApi.createRoom(homeId, floor.id, {
          roomId: roomDraft.roomId,
          fillColor: roomDraft.fillColor || null,
          polygon: roomDraft.polygon,
        });
        savedRoomId = response.id;
      }

      setIsRoomModalOpen(false);
      if (roomModalMode === "create" && availableRooms.length > 1) {
        editor.startDrawingRoom();
      } else {
        editor.enterPlaceDeviceMode();
      }
      if (savedRoomId) editor.selectRoom(savedRoomId);
      await Promise.all([reloadFloor(true), reloadFloors(true)]);
    } catch (error) {
      setRoomError((error as Error).message || t("errors.saveRoomFailed"));
    } finally {
      setIsSavingRoom(false);
    }
  }, [
    availableRooms.length,
    editor,
    floor,
    homeId,
    markExpectedLocalUpdate,
    reloadFloor,
    reloadFloors,
    roomDraft,
    roomModalMode,
    setIsRoomModalOpen,
    setIsSavingRoom,
    setRoomError,
    t,
  ]);

  const deleteRoom = useCallback(async () => {
    if (!homeId || !floor || !roomDraft.floorPlanRoomId) return;
    if (!window.confirm(t("confirmDeleteRoom"))) return;

    setIsDeletingRoom(true);
    try {
      markExpectedLocalUpdate("RoomRemoved");
      await floorsApi.removeRoom(homeId, floor.id, roomDraft.floorPlanRoomId);
      setIsRoomModalOpen(false);
      editor.clearSelection();
      await Promise.all([reloadFloor(true), reloadFloors(true)]);
    } catch (error) {
      setRoomError((error as Error).message || t("errors.deleteRoomFailed"));
    } finally {
      setIsDeletingRoom(false);
    }
  }, [
    editor,
    floor,
    homeId,
    markExpectedLocalUpdate,
    reloadFloor,
    reloadFloors,
    roomDraft.floorPlanRoomId,
    setIsDeletingRoom,
    setIsRoomModalOpen,
    setRoomError,
    t,
  ]);

  return {
    cancelDrawingRoom,
    closeRoomModal,
    createRoom,
    deleteRoom,
    editRoom,
    finishDrawingRoom,
    isDeletingRoom,
    isRoomModalOpen,
    isSavingRoom,
    roomDraft,
    roomError,
    roomModalMode,
    saveRoom,
    setRoomDraft,
  };
}
