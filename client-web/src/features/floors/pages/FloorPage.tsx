import { useCallback, useEffect, useMemo, useRef, useState, type SyntheticEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Button } from "@/shared/ui/Button";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Spinner } from "@/shared/ui/Spinner";
import { useToast } from "@/shared/ui/Toast";
import { useHomeDetail } from "@/features/homes";
import sharedStyles from "@/shared/styles/featurePage.module.css";
import { ApiError } from "@/shared/api/http";
import { subscribeToRealtimeDeltas } from "@/shared/api/sse";
import {
  DEFAULT_CANVAS_HEIGHT,
  DEFAULT_CANVAS_WIDTH,
  DEFAULT_ROOM_COLOR,
  MIN_CANVAS_HEIGHT,
  MIN_CANVAS_WIDTH,
} from "../services/floorConstants";
import {
  createEmptyRoomDraft,
  normalizeFloorReason,
  parseFloorCanvasSize,
} from "../services/floorFormService";
import { findFloorRoomIdAtPoint } from "../services/floorGeometry";
import { floorsApi } from "../api/floorsApi";
import { CanvasToolbar } from "../components/canvas/CanvasToolbar";
import { FloorCanvas } from "../components/canvas/FloorCanvas";
import { DeviceControlPanel } from "../components/panels/DeviceControlPanel";
import { FloorInfoModal } from "../components/panels/FloorInfoModal";
import { InspectorPanel } from "../components/panels/InspectorPanel";
import { RoomFormModal } from "../components/panels/RoomFormModal";
import { UnplacedFloorDevicesPanel } from "../components/panels/UnplacedFloorDevicesPanel";
import { FloorSetupPrompt } from "../components/setup/FloorSetupPrompt";
import { useFloor } from "../hooks/useFloor";
import { useFloorDevices } from "../hooks/useFloorDevices";
import { useFloorEditor } from "../hooks/useFloorEditor";
import { useFloors } from "../hooks/useFloors";
import type {
  CanvasDevice,
  FloorUpdatedReason,
  Point,
  RoomFormDraft,
} from "../types/floorTypes";
import pageStyles from "./FloorPage.module.css";

const LOCAL_FLOOR_EVENT_TTL_MS = 8_000;

type ExpectedLocalFloorUpdate = {
  reason: FloorUpdatedReason;
  expiresAt: number;
};

export function FloorPage() {
  const { homeId, floorId } = useParams();
  const navigate = useNavigate();
  const { t } = useTranslation("floors");
  const { pushToast } = useToast();
  const editor = useFloorEditor();

  const {
    home,
    isLoading: isHomeLoading,
    error: homeError,
    reload: reloadHome,
  } = useHomeDetail(homeId ?? null);
  const {
    floors,
    isLoading: isFloorsLoading,
    error: floorsError,
    reload: reloadFloors,
    setFloors,
  } = useFloors(homeId ?? null);
  const {
    floor,
    isLoading: isFloorLoading,
    error: floorError,
    reload: reloadFloor,
    setFloor,
  } = useFloor(homeId ?? null, floorId ?? null);
  const {
    devices,
    devicesById,
    error: devicesError,
    reload: reloadDevices,
    applyDeviceRealtimeDelta,
  } = useFloorDevices(homeId ?? null);

  const selectedFloorSummary = useMemo(
    () =>
      floorId
        ? floors.find((floorSummary) => floorSummary.id === floorId) ?? null
        : floors[0] ?? null,
    [floorId, floors]
  );
  const currentFloor =
    floor && (!floorId || floor.id === floorId) ? floor : null;

  const [setupName, setSetupName] = useState("");
  const [setupCanvasWidth, setSetupCanvasWidth] = useState(String(DEFAULT_CANVAS_WIDTH));
  const [setupCanvasHeight, setSetupCanvasHeight] = useState(String(DEFAULT_CANVAS_HEIGHT));
  const [setupError, setSetupError] = useState<string | null>(null);
  const [isCreatingFloor, setIsCreatingFloor] = useState(false);
  const [isCreateFloorModalOpen, setIsCreateFloorModalOpen] = useState(false);
  const [isReorderingFloors, setIsReorderingFloors] = useState(false);
  const [draggedFloorId, setDraggedFloorId] = useState<string | null>(null);

  const [isInfoModalOpen, setIsInfoModalOpen] = useState(false);
  const [infoName, setInfoName] = useState("");
  const [infoCanvasWidth, setInfoCanvasWidth] = useState("");
  const [infoCanvasHeight, setInfoCanvasHeight] = useState("");
  const [infoError, setInfoError] = useState<string | null>(null);
  const [isSavingInfo, setIsSavingInfo] = useState(false);
  const [isDeletingFloor, setIsDeletingFloor] = useState(false);

  const [isRoomModalOpen, setIsRoomModalOpen] = useState(false);
  const [roomModalMode, setRoomModalMode] = useState<"create" | "edit">("create");
  const [roomDraft, setRoomDraft] = useState<RoomFormDraft>(createEmptyRoomDraft([]));
  const [roomError, setRoomError] = useState<string | null>(null);
  const [isSavingRoom, setIsSavingRoom] = useState(false);
  const [isDeletingRoom, setIsDeletingRoom] = useState(false);

  const [controlPanelDeviceId, setControlPanelDeviceId] = useState<string | null>(null);
  const [pendingPlacementDeviceId, setPendingPlacementDeviceId] = useState<string | null>(null);
  const [pendingExternalUpdate, setPendingExternalUpdate] =
    useState<FloorUpdatedReason | null>(null);
  const [isRemovingPlacedFloorDevice, setIsRemovingPlacedFloorDevice] = useState(false);

  const editingSessionActive =
    editor.mode !== "view" || isRoomModalOpen || isInfoModalOpen;
  const editingSessionRef = useRef(editingSessionActive);
  const controlPanelDeviceIdRef = useRef<string | null>(controlPanelDeviceId);
  const expectedLocalFloorUpdatesRef = useRef<ExpectedLocalFloorUpdate[]>([]);

  useEffect(() => {
    editingSessionRef.current = editingSessionActive;
  }, [editingSessionActive]);

  useEffect(() => {
    controlPanelDeviceIdRef.current = controlPanelDeviceId;
  }, [controlPanelDeviceId]);

  useEffect(() => {
    if (home && (!currentFloor || isCreateFloorModalOpen)) {
      setSetupName((current) =>
        current.trim() !== ""
          ? current
          : t("setup.defaultName", {
            homeName: home.name,
            number: floors.length + 1,
          })
      );
    }
  }, [currentFloor, floors.length, home, isCreateFloorModalOpen, t]);

  useEffect(() => {
    if (!homeId || isFloorsLoading) {
      return;
    }

    if (!floorId && floors.length > 0) {
      navigate(`/homes/${homeId}/floors/${floors[0].id}`, { replace: true });
      return;
    }

    if (
      floorId &&
      floors.length > 0 &&
      !floors.some((floorSummary) => floorSummary.id === floorId)
    ) {
      navigate(`/homes/${homeId}/floors/${floors[0].id}`, { replace: true });
    }
  }, [floorId, floors, homeId, isFloorsLoading, navigate]);

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

  const markExpectedLocalFloorUpdate = useCallback(
    (reason: FloorUpdatedReason) => {
      const now = Date.now();
      expectedLocalFloorUpdatesRef.current = [
        ...expectedLocalFloorUpdatesRef.current.filter(
          (update) => update.expiresAt > now
        ),
        {
          reason,
          expiresAt: now + LOCAL_FLOOR_EVENT_TTL_MS,
        },
      ];
    },
    []
  );

  const consumeExpectedLocalFloorUpdate = useCallback(
    (reason: FloorUpdatedReason) => {
      const now = Date.now();
      const pendingUpdates = expectedLocalFloorUpdatesRef.current.filter(
        (update) => update.expiresAt > now
      );
      const matchingIndex = pendingUpdates.findIndex(
        (update) => update.reason === reason
      );

      if (matchingIndex === -1) {
        expectedLocalFloorUpdatesRef.current = pendingUpdates;
        return false;
      }

      pendingUpdates.splice(matchingIndex, 1);
      expectedLocalFloorUpdatesRef.current = pendingUpdates;
      return true;
    },
    []
  );

  useEffect(() => {
    if (!homeId) {
      return undefined;
    }

    const cleanup = subscribeToRealtimeDeltas({
      path: `/homes/${homeId}/events`,
      onDelta: (event) => {
        if (event.homeId && event.homeId !== homeId) {
          return;
        }

        if (event.entity === "Device" || event.entity === "DeviceCapability") {
          if (event.entity === "Device" && event.change === "Created") {
            void reloadDevices(true);
            return;
          }

          applyDeviceRealtimeDelta(event);

          if (
            event.entity === "Device" &&
            event.change === "Deleted" &&
            event.deviceId &&
            controlPanelDeviceIdRef.current === event.deviceId
          ) {
            setControlPanelDeviceId(null);
          }

          return;
        }

        if (event.entity === "Floor") {
          const delta =
            event.delta && typeof event.delta === "object" && !Array.isArray(event.delta)
              ? (event.delta as Record<string, unknown>)
              : {};
          const reason = normalizeFloorReason(
            typeof delta.reason === "string" ? delta.reason : null
          );
          const listChanged = delta.listChanged === true;
          const changedFloorId = event.floorId?.trim();

          if (changedFloorId && changedFloorId !== floorId) {
            if (listChanged) {
              void reloadFloors(true);
            }
            return;
          }

          if (consumeExpectedLocalFloorUpdate(reason)) {
            return;
          }

          if (listChanged || event.change === "Created" || event.change === "Deleted") {
            void reloadFloors(true);
          }

          if (editingSessionRef.current) {
            setPendingExternalUpdate((current) => current ?? reason);
            return;
          }

          void reloadFloor(true);
        }
      },
      onReconnect: () => {
        if (editingSessionRef.current) {
          return;
        }

        void reloadHome(true);
        void reloadFloors(true);
        void reloadDevices(true);

        void reloadFloor(true);
      },
    });

    return () => {
      cleanup();
    };
  }, [
    applyDeviceRealtimeDelta,
    consumeExpectedLocalFloorUpdate,
    floorId,
    homeId,
    reloadDevices,
    reloadFloor,
    reloadFloors,
    reloadHome,
  ]);

  const canvasDevices = useMemo<CanvasDevice[]>(() => {
    if (!currentFloor) {
      return [];
    }

    return currentFloor.placedFloorDevices.map((placedFloorDevice) => {
      const snapshot = devicesById.get(placedFloorDevice.deviceId) ?? null;

      return {
        ...placedFloorDevice,
        isOnline: snapshot?.isOnline ?? placedFloorDevice.isOnline,
        displayName:
          snapshot?.name?.trim() ||
          placedFloorDevice.deviceName?.trim() ||
          t("device.deletedLabel"),
        deviceSnapshot: snapshot,
      };
    });
  }, [currentFloor, devicesById, t]);

  const unplacedFloorDevices = useMemo(() => {
    const placedFloorDeviceIds = new Set(
      floors.flatMap((floorSummary) => floorSummary.placedDeviceIds)
    );

    return devices.filter((device) => !placedFloorDeviceIds.has(device.id));
  }, [devices, floors]);

  useEffect(() => {
    if (editor.mode !== "place-device") {
      setPendingPlacementDeviceId(null);
    }
  }, [editor.mode]);

  useEffect(() => {
    if (
      pendingPlacementDeviceId &&
      !unplacedFloorDevices.some((device) => device.id === pendingPlacementDeviceId)
    ) {
      setPendingPlacementDeviceId(null);
    }
  }, [pendingPlacementDeviceId, unplacedFloorDevices]);

  const selectedRoom = useMemo(
    () =>
      currentFloor?.rooms.find((room) => room.id === editor.selectedRoomId) ?? null,
    [currentFloor?.rooms, editor.selectedRoomId]
  );

  const selectedPlacedFloorDevice = useMemo(
    () =>
      currentFloor?.placedFloorDevices.find(
        (placedFloorDevice) => placedFloorDevice.id === editor.selectedPlacedFloorDeviceId
      ) ?? null,
    [currentFloor?.placedFloorDevices, editor.selectedPlacedFloorDeviceId]
  );

  const selectedCanvasDevice = useMemo(
    () =>
      canvasDevices.find((device) => device.id === editor.selectedPlacedFloorDeviceId) ?? null,
    [canvasDevices, editor.selectedPlacedFloorDeviceId]
  );

  const handleOpenInfoModal = useCallback(() => {
    if (!currentFloor) {
      return;
    }

    setInfoName(currentFloor.name);
    setInfoCanvasWidth(String(currentFloor.canvasWidth));
    setInfoCanvasHeight(String(currentFloor.canvasHeight));
    setInfoError(null);
    setIsInfoModalOpen(true);
  }, [currentFloor]);

  const handleCreateFloor = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!homeId) {
        return;
      }

      const name = setupName.trim();
      const { canvasWidth, canvasHeight } = parseFloorCanvasSize(
        setupCanvasWidth,
        setupCanvasHeight
      );

      if (!name) {
        setSetupError(t("errors.nameRequired"));
        return;
      }

      if (!canvasWidth) {
        setSetupError(t("errors.invalidWidth"));
        return;
      }

      if (!canvasHeight) {
        setSetupError(t("errors.invalidHeight"));
        return;
      }

      setIsCreatingFloor(true);
      setSetupError(null);

      try {
        markExpectedLocalFloorUpdate("Created");
        const response = await floorsApi.create(homeId, {
          name,
          canvasWidth,
          canvasHeight,
        });
        setIsCreateFloorModalOpen(false);
        setSetupName("");
        await reloadFloors(true);
        navigate(`/homes/${homeId}/floors/${response.id}`);
      } catch (error) {
        setSetupError(
          (error as Error).message || t("errors.createFloorFailed")
        );
      } finally {
        setIsCreatingFloor(false);
      }
    },
    [
      homeId,
      markExpectedLocalFloorUpdate,
      navigate,
      reloadFloors,
      setupCanvasHeight,
      setupCanvasWidth,
      setupName,
      t,
    ]
  );

  const handleSaveFloorInfo = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!homeId || !currentFloor) {
        return;
      }

      const name = infoName.trim();
      const { canvasWidth, canvasHeight } = parseFloorCanvasSize(
        infoCanvasWidth,
        infoCanvasHeight
      );

      if (!name) {
        setInfoError(t("errors.nameRequired"));
        return;
      }

      if (!canvasWidth) {
        setInfoError(t("errors.invalidWidth"));
        return;
      }

      if (!canvasHeight) {
        setInfoError(t("errors.invalidHeight"));
        return;
      }

      setIsSavingInfo(true);
      setInfoError(null);

      try {
        markExpectedLocalFloorUpdate("InfoUpdated");
        await floorsApi.updateInfo(homeId, currentFloor.id, {
          name,
          canvasWidth,
          canvasHeight,
        });
        setIsInfoModalOpen(false);
        await Promise.all([reloadFloor(true), reloadFloors(true)]);
      } catch (error) {
        setInfoError(
          (error as Error).message || t("errors.updateFloorFailed")
        );
      } finally {
        setIsSavingInfo(false);
      }
    },
    [
      currentFloor,
      homeId,
      infoCanvasHeight,
      infoCanvasWidth,
      infoName,
      markExpectedLocalFloorUpdate,
      reloadFloor,
      reloadFloors,
      t,
    ]
  );

  const handleDeleteFloor = useCallback(async () => {
    if (!homeId || !currentFloor) {
      return;
    }

    if (!window.confirm(t("confirmDeleteFloor"))) {
      return;
    }

    setIsDeletingFloor(true);

    try {
      markExpectedLocalFloorUpdate("Deleted");
      await floorsApi.delete(homeId, currentFloor.id);
      const nextFloors = await reloadFloors(true);
      const deletedIndex = floors.findIndex((floorSummary) => floorSummary.id === currentFloor.id);
      const nextFloor = nextFloors[deletedIndex] ?? nextFloors[deletedIndex - 1] ?? null;

      setFloor(null);
      setControlPanelDeviceId(null);
      setPendingExternalUpdate(null);
      setIsInfoModalOpen(false);
      editor.enterViewMode();
      navigate(
        nextFloor
          ? `/homes/${homeId}/floors/${nextFloor.id}`
          : `/homes/${homeId}/floors`,
        { replace: true }
      );
    } catch (error) {
      pushToast({
        tone: "error",
        message:
          (error as Error).message || t("errors.deleteFloorFailed"),
      });
    } finally {
      setIsDeletingFloor(false);
    }
  }, [
    editor,
    currentFloor,
    floors,
    homeId,
    markExpectedLocalFloorUpdate,
    navigate,
    pushToast,
    reloadFloors,
    setFloor,
    t,
  ]);

  const handleCreateRoom = useCallback((polygon: Point[]) => {
    setRoomModalMode("create");
    setRoomDraft(createEmptyRoomDraft(polygon));
    setRoomError(null);
    setIsRoomModalOpen(true);
  }, []);

  const handleFinishDrawingRoom = useCallback(() => {
    const polygon = editor.finishDrawing();

    if (polygon) {
      handleCreateRoom(polygon);
    }
  }, [editor, handleCreateRoom]);

  const handleCancelDrawingRoom = useCallback(() => {
    editor.cancelDrawing("place-device");
  }, [editor]);

  const handleSelectDeviceForPlacement = useCallback(
    (deviceId: string) => {
      editor.enterPlaceDeviceMode();
      editor.clearSelection();
      setPendingPlacementDeviceId(deviceId);
    },
    [editor]
  );

  const handleEditRoom = useCallback(
    (roomId: string) => {
      const room = currentFloor?.rooms.find((entry) => entry.id === roomId);

      if (!room) {
        return;
      }

      editor.selectRoom(roomId);
      setRoomModalMode("edit");
      setRoomDraft({
        roomId: room.id,
        linkedRoomId: room.linkedRoomId ?? "",
        label: room.label,
        fillColor: room.fillColor ?? DEFAULT_ROOM_COLOR,
        polygon: room.polygon,
      });
      setRoomError(null);
      setIsRoomModalOpen(true);
    },
    [currentFloor?.rooms, editor]
  );

  const handleSaveRoom = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!homeId || !currentFloor) {
        return;
      }

      const label = roomDraft.label.trim();

      if (!label) {
        setRoomError(t("errors.roomLabelRequired"));
        return;
      }

      if (roomDraft.polygon.length < 3) {
        setRoomError(t("errors.roomPolygonRequired"));
        return;
      }

      setIsSavingRoom(true);
      setRoomError(null);

      try {
        let savedRoomId = roomDraft.roomId;

        if (roomModalMode === "edit" && roomDraft.roomId) {
          markExpectedLocalFloorUpdate("RoomUpdated");
          const response = await floorsApi.updateRoom(homeId, currentFloor.id, roomDraft.roomId, {
            label,
            linkedRoomId: roomDraft.linkedRoomId || null,
            fillColor: roomDraft.fillColor || null,
            polygon: roomDraft.polygon,
          });
          savedRoomId = response.id;
        } else {
          markExpectedLocalFloorUpdate("RoomAdded");
          const response = await floorsApi.createRoom(homeId, currentFloor.id, {
            label,
            linkedRoomId: roomDraft.linkedRoomId || null,
            fillColor: roomDraft.fillColor || null,
            polygon: roomDraft.polygon,
          });
          savedRoomId = response.id;
        }

        setIsRoomModalOpen(false);
        if (roomModalMode === "create") {
          editor.startDrawingRoom();
        } else {
          editor.enterPlaceDeviceMode();
        }
        if (savedRoomId) {
          editor.selectRoom(savedRoomId);
        }
        await Promise.all([reloadFloor(true), reloadFloors(true)]);
      } catch (error) {
        setRoomError((error as Error).message || t("errors.saveRoomFailed"));
      } finally {
        setIsSavingRoom(false);
      }
    },
    [
      editor,
      currentFloor,
      homeId,
      markExpectedLocalFloorUpdate,
      reloadFloor,
      reloadFloors,
      roomDraft,
      roomModalMode,
      t,
    ]
  );

  const handleDeleteRoom = useCallback(async () => {
    if (!homeId || !currentFloor || !roomDraft.roomId) {
      return;
    }

    if (!window.confirm(t("confirmDeleteRoom"))) {
      return;
    }

    setIsDeletingRoom(true);

    try {
      markExpectedLocalFloorUpdate("RoomRemoved");
      await floorsApi.removeRoom(homeId, currentFloor.id, roomDraft.roomId);
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
    currentFloor,
    homeId,
    markExpectedLocalFloorUpdate,
    reloadFloor,
    reloadFloors,
    roomDraft.roomId,
    t,
  ]);

  const handlePlaceDevice = useCallback(
    async (deviceId: string, point: Point) => {
      if (!homeId || !currentFloor) {
        return;
      }

      try {
        markExpectedLocalFloorUpdate("DevicePlaced");
        const response = await floorsApi.placeDevice(homeId, currentFloor.id, {
          deviceId,
          x: point.x,
          y: point.y,
          floorRoomId: findFloorRoomIdAtPoint(currentFloor.rooms, point),
        });

        await Promise.all([reloadFloor(true), reloadFloors(true)]);
        setPendingPlacementDeviceId(null);
        editor.selectPlacedFloorDevice(response.id);
      } catch (error) {
        pushToast({
          tone: "error",
          message: (error as Error).message || t("errors.placeDeviceFailed"),
        });
      }
    },
    [
      editor,
      currentFloor,
      homeId,
      markExpectedLocalFloorUpdate,
      pushToast,
      reloadFloor,
      reloadFloors,
      t,
    ]
  );

  const handleMovePlacedFloorDevice = useCallback(
    async (placedFloorDeviceId: string, point: Point) => {
      if (!homeId || !currentFloor) {
        return;
      }

      try {
        markExpectedLocalFloorUpdate("DeviceMoved");
        await floorsApi.moveDevice(homeId, currentFloor.id, placedFloorDeviceId, {
          x: point.x,
          y: point.y,
          floorRoomId: findFloorRoomIdAtPoint(currentFloor.rooms, point),
        });
        await reloadFloor(true);
      } catch (error) {
        pushToast({
          tone: "error",
          message: (error as Error).message || t("errors.moveDeviceFailed"),
        });
        await reloadFloor(true);
      }
    },
    [
      currentFloor,
      homeId,
      markExpectedLocalFloorUpdate,
      pushToast,
      reloadFloor,
      t,
    ]
  );

  const handleRemovePlacedFloorDevice = useCallback(
    async (placedFloorDeviceId: string) => {
      if (!homeId || !currentFloor) {
        return;
      }

      if (!window.confirm(t("confirmRemovePlacedFloorDevice"))) {
        return;
      }

      setIsRemovingPlacedFloorDevice(true);

      try {
        markExpectedLocalFloorUpdate("DeviceRemoved");
        await floorsApi.removePlacedFloorDevice(homeId, currentFloor.id, placedFloorDeviceId);
        editor.clearSelection();
        await Promise.all([reloadFloor(true), reloadFloors(true)]);
      } catch (error) {
        pushToast({
          tone: "error",
          message:
            (error as Error).message || t("errors.removePlacedFloorDeviceFailed"),
        });
      } finally {
        setIsRemovingPlacedFloorDevice(false);
      }
    },
    [
      editor,
      currentFloor,
      homeId,
      markExpectedLocalFloorUpdate,
      pushToast,
      reloadFloor,
      reloadFloors,
      t,
    ]
  );

  const handleDeviceClick = useCallback(
    (placedFloorDeviceId: string) => {
      if (!currentFloor) {
        return;
      }

      const placedFloorDevice = currentFloor.placedFloorDevices.find(
        (device) => device.id === placedFloorDeviceId
      );

      if (!placedFloorDevice) {
        return;
      }

      if (editor.mode === "view") {
        if (placedFloorDevice.isDeleted) {
          pushToast({
            tone: "info",
            message: t("device.deletedLabel"),
          });
          return;
        }

        setControlPanelDeviceId(placedFloorDevice.deviceId);
        return;
      }

      if (editor.mode === "place-device") {
        setPendingPlacementDeviceId(null);
        editor.selectPlacedFloorDevice(placedFloorDeviceId);
      }
    },
    [currentFloor, editor, pushToast, t]
  );

  const handleRoomClick = useCallback(
    (roomId: string) => {
      if (editor.mode !== "place-device") {
        return;
      }

      setPendingPlacementDeviceId(null);
      editor.selectRoom(roomId);
    },
    [editor]
  );

  const handleReloadExternalChanges = useCallback(async () => {
    setPendingExternalUpdate(null);
    setIsRoomModalOpen(false);
    setIsInfoModalOpen(false);
    setControlPanelDeviceId(null);
    editor.enterViewMode();
    await reloadAll(true);
  }, [editor, reloadAll]);

  const handleOpenCreateFloorModal = useCallback(() => {
    const previousFloor = floors[floors.length - 1] ?? currentFloor ?? null;

    setSetupName(
      home
        ? t("setup.defaultName", {
          homeName: home.name,
          number: floors.length + 1,
        })
        : ""
    );
    setSetupCanvasWidth(
      String(previousFloor?.canvasWidth ?? DEFAULT_CANVAS_WIDTH)
    );
    setSetupCanvasHeight(
      String(previousFloor?.canvasHeight ?? DEFAULT_CANVAS_HEIGHT)
    );
    setSetupError(null);
    setIsCreateFloorModalOpen(true);
  }, [currentFloor, floors, home, t]);

  const handleFloorDrop = useCallback(
    async (targetFloorId: string) => {
      if (!homeId || !draggedFloorId || draggedFloorId === targetFloorId) {
        setDraggedFloorId(null);
        return;
      }

      const draggedIndex = floors.findIndex((floorSummary) => floorSummary.id === draggedFloorId);
      const targetIndex = floors.findIndex((floorSummary) => floorSummary.id === targetFloorId);

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
    },
    [draggedFloorId, floors, homeId, pushToast, reloadFloors, setFloors, t]
  );

  const isLoading = isHomeLoading || isFloorsLoading;
  const isFloorContentLoading = Boolean(floorId) && isFloorLoading && !currentFloor;
  const shouldShowFloorPlaceholder = !currentFloor && floors.length > 0;
  const pageTitle =
    currentFloor?.name ?? selectedFloorSummary?.name ?? t("pageTitleFallback");
  const isHomeNotFound =
    homeError instanceof ApiError && homeError.status === 404;

  if (!homeId) {
    return <div className={sharedStyles.emptyState}>{t("notFound")}</div>;
  }

  if (isLoading) {
    return (
      <div className={pageStyles.loadingState}>
        <Spinner />
      </div>
    );
  }

  if (homeError) {
    return (
      <div className={sharedStyles.emptyState}>
        {isHomeNotFound ? t("notFound") : t("failed")}
      </div>
    );
  }

  if (!home) {
    return <div className={sharedStyles.emptyState}>{t("notFound")}</div>;
  }

  if (floorsError && floors.length === 0) {
    return <div className={sharedStyles.emptyState}>{t("failed")}</div>;
  }

  if (floorId && floorError && !currentFloor) {
    return <div className={sharedStyles.emptyState}>{t("failed")}</div>;
  }

  return (
    <div className={sharedStyles.pageStack}>
      <PageHeader
        title={
          <span className={pageStyles.pageTitle}>
            <span>{pageTitle}</span>
            <span className={pageStyles.pageSubtitle}>{home.name}</span>
          </span>
        }
        action={
          <div className={pageStyles.headerActions}>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => navigate(`/homes/${home.id}`)}
            >
              {t("actions.backToHome")}
            </Button>
            <Button
              size="sm"
              onClick={handleOpenCreateFloorModal}
            >
              {t("actions.createFloor")}
            </Button>
            {currentFloor ? (
              <>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={handleOpenInfoModal}
                >
                  {t("actions.editInfo")}
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  disabled={isDeletingFloor}
                  onClick={() => void handleDeleteFloor()}
                >
                  {isDeletingFloor
                    ? t("actions.deletingFloor")
                    : t("actions.deleteFloor")}
                </Button>
              </>
            ) : null}
          </div>
        }
      />

      {pendingExternalUpdate ? (
        <section className={pageStyles.noticeBar}>
          <div className={pageStyles.noticeCopy}>
            <strong>{t("events.externalUpdateTitle")}</strong>
            <span>
              {t(`events.reasons.${pendingExternalUpdate}`, {
                defaultValue: t("events.reasons.Unknown"),
              })}
            </span>
          </div>
          <div className={pageStyles.noticeActions}>
            <Button size="sm" onClick={() => void handleReloadExternalChanges()}>
              {t("events.reloadNow")}
            </Button>
            <Button
              size="sm"
              variant="secondary"
              onClick={() => setPendingExternalUpdate(null)}
            >
              {t("events.keepEditing")}
            </Button>
          </div>
        </section>
      ) : null}

      {floors.length > 0 ? (
        <nav className={pageStyles.floorTabs} aria-label={t("floorTabs.label")}>
          {floors.map((floorSummary) => {
            const isActive = floorSummary.id === (floorId ?? currentFloor?.id);

            return (
              <button
                key={floorSummary.id}
                type="button"
                className={`${pageStyles.floorTab} ${isActive ? pageStyles.floorTabActive : ""}`}
                draggable={!isReorderingFloors}
                disabled={isReorderingFloors}
                onClick={() => navigate(`/homes/${home.id}/floors/${floorSummary.id}`)}
                onDragStart={() => setDraggedFloorId(floorSummary.id)}
                onDragEnd={() => setDraggedFloorId(null)}
                onDragOver={(event) => event.preventDefault()}
                onDrop={() => void handleFloorDrop(floorSummary.id)}
              >
                <span className={pageStyles.floorTabGrip} aria-hidden="true">::</span>
                <span className={pageStyles.floorTabName}>{floorSummary.name}</span>
                <span className={pageStyles.floorTabMeta}>
                  {t("floorTabs.meta", {
                    rooms: floorSummary.roomCount,
                    devices: floorSummary.placedDeviceCount,
                  })}
                </span>
              </button>
            );
          })}
        </nav>
      ) : null}

      {isFloorContentLoading || shouldShowFloorPlaceholder ? (
        <section className={pageStyles.floorContentLoading}>
          <Spinner />
          <span>{t("loading")}</span>
        </section>
      ) : !currentFloor ? (
        <FloorSetupPrompt
          homeName={home.name}
          name={setupName}
          canvasWidth={setupCanvasWidth}
          canvasHeight={setupCanvasHeight}
          isCreating={isCreatingFloor}
          error={setupError}
          title={t("setup.title")}
          saveLabel={t("setup.create")}
          savingLabel={t("setup.creating")}
          nameLabel={t("fields.name")}
          widthLabel={t("fields.canvasWidth")}
          heightLabel={t("fields.canvasHeight")}
          minCanvasWidth={MIN_CANVAS_WIDTH}
          minCanvasHeight={MIN_CANVAS_HEIGHT}
          onSubmit={handleCreateFloor}
          onNameChange={setSetupName}
          onCanvasWidthChange={setSetupCanvasWidth}
          onCanvasHeightChange={setSetupCanvasHeight}
        />
      ) : (
        <>
          <section className={pageStyles.floorWorkbar}>
            <CanvasToolbar
              mode={editor.mode}
              drawingPointCount={editor.drawing.points.length}
              onEnterViewMode={editor.enterViewMode}
              onEnterPlaceDeviceMode={editor.enterPlaceDeviceMode}
              onStartDrawingRoom={editor.startDrawingRoom}
              onUndoDrawingPoint={editor.removeLastDrawingPoint}
              onFinishDrawingRoom={handleFinishDrawingRoom}
              onCancelDrawingRoom={handleCancelDrawingRoom}
              viewLabel={t("toolbar.view")}
              editLabel={t("toolbar.edit")}
              drawRoomLabel={t("toolbar.drawRoom")}
              undoPointLabel={t("toolbar.undoPoint")}
              cancelDrawingLabel={t("toolbar.cancelDrawing")}
              drawingPointCountLabel={t("toolbar.pointCount", {
                count: editor.drawing.points.length,
              })}
              drawRoomHint={t("canvas.drawHint")}
              idleHint={t("toolbar.idleHint")}
            />

            <div className={pageStyles.planMeta}>
              <span className={pageStyles.metaChip}>
                {t("stats.rooms", { count: currentFloor.rooms.length })}
              </span>
              <span className={pageStyles.metaChip}>
                {t("stats.placedFloorDevices", { count: currentFloor.placedFloorDevices.length })}
              </span>
              <span className={pageStyles.metaChip}>
                {t("stats.unplacedFloorDevices", { count: unplacedFloorDevices.length })}
              </span>
            </div>
          </section>

          <div
            className={`${pageStyles.boardLayout} ${editor.isEditMode ? pageStyles.boardLayoutWithSidebar : ""}`}
          >
            <section className={pageStyles.canvasColumn}>
              <FloorCanvas
                floor={currentFloor}
                devices={canvasDevices}
                editor={editor}
                pendingPlacementDeviceId={pendingPlacementDeviceId}
                onBlankClick={editor.clearSelection}
                onCreateRoom={handleCreateRoom}
                onFinishDrawingRoom={handleFinishDrawingRoom}
                onCancelDrawingRoom={handleCancelDrawingRoom}
                onUndoDrawingPoint={editor.removeLastDrawingPoint}
                onPlaceDevice={(deviceId, point) =>
                  void handlePlaceDevice(deviceId, point)
                }
                onRoomClick={handleRoomClick}
                onDeviceClick={handleDeviceClick}
                onDeviceDragEnd={(placedFloorDeviceId, point) =>
                  void handleMovePlacedFloorDevice(placedFloorDeviceId, point)
                }
              />
            </section>

            {editor.isEditMode ? (
              <aside className={pageStyles.sideColumn}>
                {editor.mode === "place-device" ? (
                  <UnplacedFloorDevicesPanel
                    devices={unplacedFloorDevices}
                    selectedDeviceId={pendingPlacementDeviceId}
                    title={t("panels.unplacedTitle")}
                    helperText={t("panels.unplacedHelper")}
                    emptyText={t("panels.unplacedEmpty")}
                    roomFallbackLabel={t("panels.noLinkedRoom")}
                    onSelectDevice={handleSelectDeviceForPlacement}
                  />
                ) : null}

                <InspectorPanel
                  mode={editor.mode}
                  floor={currentFloor}
                  selectedRoom={selectedRoom}
                  selectedPlacedFloorDevice={selectedPlacedFloorDevice}
                  selectedCanvasDevice={selectedCanvasDevice}
                  unplacedFloorDeviceCount={unplacedFloorDevices.length}
                  isRemovingPlacedFloorDevice={isRemovingPlacedFloorDevice}
                  onEditRoom={handleEditRoom}
                  onOpenDeviceDetails={(deviceId) =>
                    navigate(`/homes/${home.id}/devices/${deviceId}`)
                  }
                  onRemovePlacedFloorDevice={(placedFloorDeviceId) =>
                    void handleRemovePlacedFloorDevice(placedFloorDeviceId)
                  }
                  drawModeTitle={t("panels.drawTitle")}
                  drawModeDescription={t("panels.drawDescription")}
                  emptyTitle={t("panels.selectionTitle")}
                  emptyDescription={t("panels.selectionDescription")}
                  roomLinkedLabel={t("panels.linkedRoom")}
                  roomNoLinkedLabel={t("panels.noLinkedRoom")}
                  deviceDeletedLabel={t("device.deletedLabel")}
                  deviceOpenDetailsLabel={t("panels.openDeviceDetails")}
                  deviceRemoveLabel={t("panels.removePlacedFloorDevice")}
                  deviceRemovingLabel={t("panels.removingPlacedFloorDevice")}
                  deviceRoomLabel={t("panels.deviceRoom")}
                  deviceHomeRoomLabel={t("panels.deviceHomeRoom")}
                  deviceStatusLabel={t("panels.deviceStatus")}
                  deviceOnlineLabel={t("device.online")}
                  deviceOfflineLabel={t("device.offline")}
                  statsTitle={t("panels.statsTitle")}
                  statsRoomsLabel={t("panels.statsRooms")}
                  statsPlacedFloorDevicesLabel={t("panels.statsPlacedFloorDevices")}
                  statsUnplacedLabel={t("panels.statsUnplacedFloorDevices")}
                  roomEditLabel={t("panels.editRoom")}
                  roomPolygonHint={t("panels.roomPoints")}
                />

                {devicesError ? (
                  <div className={pageStyles.inlineError}>
                    {devicesError.message || t("errors.loadDevicesFailed")}
                  </div>
                ) : null}
              </aside>
            ) : null}
          </div>
        </>
      )}

      <RoomFormModal
        open={isRoomModalOpen}
        title={
          roomModalMode === "create"
            ? t("roomForm.createTitle")
            : t("roomForm.editTitle")
        }
        label={roomDraft.label}
        linkedRoomId={roomDraft.linkedRoomId}
        fillColor={roomDraft.fillColor}
        polygonPointCount={roomDraft.polygon.length}
        rooms={home.rooms}
        isSaving={isSavingRoom}
        isDeleting={isDeletingRoom}
        error={roomError}
        deleteLabel={t("roomForm.delete")}
        deletingLabel={t("roomForm.deleting")}
        saveLabel={t("roomForm.save")}
        savingLabel={t("roomForm.saving")}
        cancelLabel={t("common.cancel")}
        labelFieldLabel={t("roomForm.label")}
        labelPlaceholder={t("roomForm.labelPlaceholder")}
        linkedRoomLabel={t("roomForm.linkedRoom")}
        noLinkedRoomLabel={t("roomForm.noLinkedRoom")}
        fillColorLabel={t("roomForm.fillColor")}
        polygonLabel={t("roomForm.polygon")}
        polygonHint={t("roomForm.polygonHint")}
        onClose={() => {
          setIsRoomModalOpen(false);
          setRoomError(null);
        }}
        onSubmit={handleSaveRoom}
        onDelete={roomModalMode === "edit" ? () => void handleDeleteRoom() : undefined}
        onLabelChange={(value) =>
          setRoomDraft((current) => ({ ...current, label: value }))
        }
        onLinkedRoomIdChange={(value) =>
          setRoomDraft((current) => ({ ...current, linkedRoomId: value }))
        }
        onFillColorChange={(value) =>
          setRoomDraft((current) => ({ ...current, fillColor: value }))
        }
      />

      <FloorInfoModal
        open={isCreateFloorModalOpen}
        title={t("setup.modalTitle")}
        name={setupName}
        canvasWidth={setupCanvasWidth}
        canvasHeight={setupCanvasHeight}
        isSaving={isCreatingFloor}
        error={setupError}
        saveLabel={t("setup.create")}
        savingLabel={t("setup.creating")}
        cancelLabel={t("common.cancel")}
        nameLabel={t("fields.name")}
        widthLabel={t("fields.canvasWidth")}
        heightLabel={t("fields.canvasHeight")}
        minCanvasWidth={MIN_CANVAS_WIDTH}
        minCanvasHeight={MIN_CANVAS_HEIGHT}
        helperText={t("setup.helper")}
        onClose={() => {
          setIsCreateFloorModalOpen(false);
          setSetupError(null);
        }}
        onSubmit={handleCreateFloor}
        onNameChange={setSetupName}
        onCanvasWidthChange={setSetupCanvasWidth}
        onCanvasHeightChange={setSetupCanvasHeight}
      />

      <FloorInfoModal
        open={isInfoModalOpen}
        title={t("infoModal.title")}
        name={infoName}
        canvasWidth={infoCanvasWidth}
        canvasHeight={infoCanvasHeight}
        isSaving={isSavingInfo}
        error={infoError}
        saveLabel={t("infoModal.save")}
        savingLabel={t("infoModal.saving")}
        cancelLabel={t("common.cancel")}
        nameLabel={t("fields.name")}
        widthLabel={t("fields.canvasWidth")}
        heightLabel={t("fields.canvasHeight")}
        minCanvasWidth={MIN_CANVAS_WIDTH}
        minCanvasHeight={MIN_CANVAS_HEIGHT}
        helperText={t("infoModal.helper")}
        onClose={() => {
          setIsInfoModalOpen(false);
          setInfoError(null);
        }}
        onSubmit={handleSaveFloorInfo}
        onNameChange={setInfoName}
        onCanvasWidthChange={setInfoCanvasWidth}
        onCanvasHeightChange={setInfoCanvasHeight}
      />

      {controlPanelDeviceId ? (
        <DeviceControlPanel
          homeId={home.id}
          deviceId={controlPanelDeviceId}
          onClose={() => setControlPanelDeviceId(null)}
          openDetailsLabel={t("device.openDetails")}
          notFoundLabel={t("device.notFound")}
        />
      ) : null}
    </div>
  );
}
