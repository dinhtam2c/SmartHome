import { useCallback, useState, type Dispatch, type SetStateAction, type SyntheticEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { HomeDetailDto } from "@/features/homes";
import { useToast } from "@/shared/ui/Toast";
import { floorsApi } from "../api/floorsApi";
import {
  DEFAULT_CANVAS_HEIGHT,
  DEFAULT_CANVAS_WIDTH,
} from "../services/floorConstants";
import { parseFloorCanvasSize } from "../services/floorFormService";
import type { useFloorEditor } from "./useFloorEditor";
import type { Floor, FloorSummary, FloorUpdatedReason } from "../types/floorTypes";

type EditorState = ReturnType<typeof useFloorEditor>;
type ReloadFloor = (silent?: boolean) => Promise<Floor | null>;
type ReloadFloors = (silent?: boolean) => Promise<FloorSummary[]>;

type Params = {
  homeId: string | null;
  home: HomeDetailDto | null;
  floor: Floor | null;
  floors: FloorSummary[];
  editor: EditorState;
  setFloor: Dispatch<SetStateAction<Floor | null>>;
  setFloors: Dispatch<SetStateAction<FloorSummary[]>>;
  closeControlPanel: () => void;
  markExpectedLocalUpdate: (reason: FloorUpdatedReason) => void;
  reloadFloor: ReloadFloor;
  reloadFloors: ReloadFloors;
};

export function useFloorDetailsActions({
  homeId,
  home,
  floor,
  floors,
  editor,
  setFloor,
  setFloors,
  closeControlPanel,
  markExpectedLocalUpdate,
  reloadFloor,
  reloadFloors,
}: Params) {
  const navigate = useNavigate();
  const { t } = useTranslation("floors");
  const { pushToast } = useToast();
  const [setupName, setSetupName] = useState("");
  const [setupCanvasWidth, setSetupCanvasWidth] = useState(String(DEFAULT_CANVAS_WIDTH));
  const [setupCanvasHeight, setSetupCanvasHeight] = useState(String(DEFAULT_CANVAS_HEIGHT));
  const [setupError, setSetupError] = useState<string | null>(null);
  const [isCreatingFloor, setIsCreatingFloor] = useState(false);
  const [isCreateFloorModalOpen, setIsCreateFloorModalOpen] = useState(false);
  const [infoName, setInfoName] = useState("");
  const [infoCanvasWidth, setInfoCanvasWidth] = useState("");
  const [infoCanvasHeight, setInfoCanvasHeight] = useState("");
  const [infoError, setInfoError] = useState<string | null>(null);
  const [isInfoModalOpen, setIsInfoModalOpen] = useState(false);
  const [isSavingInfo, setIsSavingInfo] = useState(false);
  const [isDeletingFloor, setIsDeletingFloor] = useState(false);
  const [draggedFloorId, setDraggedFloorId] = useState<string | null>(null);
  const [isReorderingFloors, setIsReorderingFloors] = useState(false);

  const closeCreateFloorModal = useCallback(() => {
    setIsCreateFloorModalOpen(false);
    setSetupError(null);
  }, []);

  const closeInfoModal = useCallback(() => {
    setIsInfoModalOpen(false);
    setInfoError(null);
  }, []);

  const openInfoModal = useCallback(() => {
    if (!floor) return;
    setInfoName(floor.name);
    setInfoCanvasWidth(String(floor.canvasWidth));
    setInfoCanvasHeight(String(floor.canvasHeight));
    setInfoError(null);
    setIsInfoModalOpen(true);
  }, [
    floor,
    setInfoCanvasHeight,
    setInfoCanvasWidth,
    setInfoError,
    setInfoName,
    setIsInfoModalOpen,
  ]);

  const createFloor = useCallback(async (event: SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!homeId) return;

    const defaultName = home
      ? t("setup.defaultName", { homeName: home.name, number: floors.length + 1 })
      : "";
    const name = setupName.trim() || defaultName.trim();
    const { canvasWidth, canvasHeight } = parseFloorCanvasSize(
      setupCanvasWidth,
      setupCanvasHeight
    );
    if (!name) return setSetupError(t("errors.nameRequired"));
    if (!canvasWidth) return setSetupError(t("errors.invalidWidth"));
    if (!canvasHeight) return setSetupError(t("errors.invalidHeight"));

    setIsCreatingFloor(true);
    setSetupError(null);
    try {
      markExpectedLocalUpdate("Created");
      const response = await floorsApi.create(homeId, { name, canvasWidth, canvasHeight });
      setIsCreateFloorModalOpen(false);
      setSetupName("");
      await reloadFloors(true);
      navigate(`/homes/${homeId}/floors/${response.id}`);
    } catch (error) {
      setSetupError((error as Error).message || t("errors.createFloorFailed"));
    } finally {
      setIsCreatingFloor(false);
    }
  }, [
    homeId,
    home,
    floors.length,
    markExpectedLocalUpdate,
    navigate,
    reloadFloors,
    setIsCreateFloorModalOpen,
    setIsCreatingFloor,
    setSetupError,
    setSetupName,
    setupCanvasHeight,
    setupCanvasWidth,
    setupName,
    t,
  ]);

  const saveFloorInfo = useCallback(async (event: SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!homeId || !floor) return;

    const name = infoName.trim();
    const { canvasWidth, canvasHeight } = parseFloorCanvasSize(
      infoCanvasWidth,
      infoCanvasHeight
    );
    if (!name) return setInfoError(t("errors.nameRequired"));
    if (!canvasWidth) return setInfoError(t("errors.invalidWidth"));
    if (!canvasHeight) return setInfoError(t("errors.invalidHeight"));

    setIsSavingInfo(true);
    setInfoError(null);
    try {
      markExpectedLocalUpdate("InfoUpdated");
      await floorsApi.updateInfo(homeId, floor.id, { name, canvasWidth, canvasHeight });
      setIsInfoModalOpen(false);
      await Promise.all([reloadFloor(true), reloadFloors(true)]);
    } catch (error) {
      setInfoError((error as Error).message || t("errors.updateFloorFailed"));
    } finally {
      setIsSavingInfo(false);
    }
  }, [
    floor,
    homeId,
    infoCanvasHeight,
    infoCanvasWidth,
    infoName,
    markExpectedLocalUpdate,
    reloadFloor,
    reloadFloors,
    setInfoError,
    setIsInfoModalOpen,
    setIsSavingInfo,
    t,
  ]);

  const deleteFloor = useCallback(async () => {
    if (!homeId || !floor || !window.confirm(t("confirmDeleteFloor"))) return;

    setIsDeletingFloor(true);
    try {
      markExpectedLocalUpdate("Deleted");
      await floorsApi.delete(homeId, floor.id);
      const nextFloors = await reloadFloors(true);
      const deletedIndex = floors.findIndex((item) => item.id === floor.id);
      const nextFloor = nextFloors[deletedIndex] ?? nextFloors[deletedIndex - 1] ?? null;

      setFloor(null);
      closeControlPanel();
      setIsInfoModalOpen(false);
      editor.enterViewMode();
      navigate(
        nextFloor ? `/homes/${homeId}/floors/${nextFloor.id}` : `/homes/${homeId}/floors`,
        { replace: true }
      );
    } catch (error) {
      pushToast({
        tone: "error",
        message: (error as Error).message || t("errors.deleteFloorFailed"),
      });
    } finally {
      setIsDeletingFloor(false);
    }
  }, [
    editor,
    floor,
    floors,
    homeId,
    markExpectedLocalUpdate,
    navigate,
    pushToast,
    reloadFloors,
    closeControlPanel,
    setFloor,
    setIsDeletingFloor,
    setIsInfoModalOpen,
    t,
  ]);

  const openCreateFloorModal = useCallback(() => {
    const previousFloor = floors[floors.length - 1] ?? floor;
    setSetupName(
      home
        ? t("setup.defaultName", { homeName: home.name, number: floors.length + 1 })
        : ""
    );
    setSetupCanvasWidth(String(previousFloor?.canvasWidth ?? DEFAULT_CANVAS_WIDTH));
    setSetupCanvasHeight(String(previousFloor?.canvasHeight ?? DEFAULT_CANVAS_HEIGHT));
    setSetupError(null);
    setIsCreateFloorModalOpen(true);
  }, [
    floor,
    floors,
    home,
    setIsCreateFloorModalOpen,
    setSetupCanvasHeight,
    setSetupCanvasWidth,
    setSetupError,
    setSetupName,
    t,
  ]);

  const reorderFloor = useCallback(async (targetFloorId: string) => {
    if (!homeId || !draggedFloorId || draggedFloorId === targetFloorId) {
      setDraggedFloorId(null);
      return;
    }

    const draggedIndex = floors.findIndex((item) => item.id === draggedFloorId);
    const targetIndex = floors.findIndex((item) => item.id === targetFloorId);
    if (draggedIndex < 0 || targetIndex < 0) {
      setDraggedFloorId(null);
      return;
    }

    const nextFloors = [...floors];
    const [movedFloor] = nextFloors.splice(draggedIndex, 1);
    nextFloors.splice(targetIndex, 0, movedFloor);
    setFloors(nextFloors.map((item, index) => ({ ...item, sortOrder: index + 1 })));
    setDraggedFloorId(null);
    setIsReorderingFloors(true);

    try {
      await floorsApi.reorder(homeId, nextFloors.map((item) => item.id));
      await reloadFloors(true);
    } catch (error) {
      pushToast({
        tone: "error",
        message: (error as Error).message || t("errors.reorderFloorsFailed"),
      });
      await reloadFloors(true);
    } finally {
      setIsReorderingFloors(false);
    }
  }, [
    draggedFloorId,
    floors,
    homeId,
    pushToast,
    reloadFloors,
    setDraggedFloorId,
    setFloors,
    setIsReorderingFloors,
    t,
  ]);

  return {
    closeCreateFloorModal,
    closeInfoModal,
    createFloor,
    deleteFloor,
    infoCanvasHeight,
    infoCanvasWidth,
    infoError,
    infoName,
    isCreateFloorModalOpen,
    isCreatingFloor,
    isDeletingFloor,
    isInfoModalOpen,
    isReorderingFloors,
    isSavingInfo,
    openCreateFloorModal,
    openInfoModal,
    reorderFloor,
    saveFloorInfo,
    setDraggedFloorId,
    setInfoCanvasHeight,
    setInfoCanvasWidth,
    setInfoName,
    setSetupCanvasHeight,
    setSetupCanvasWidth,
    setSetupName,
    setupCanvasHeight,
    setupCanvasWidth,
    setupError,
    setupName,
  };
}
