import { useCallback, useState } from "react";
import type { SyntheticEvent } from "react";
import type { TFunction } from "i18next";
import { useToast } from "@/components/Toast";
import { useCapabilityRegistry } from "@/features/capabilities";
import { createDevice } from "@/features/devices/devices.api";
import { createRoom, updateRoom } from "@/features/rooms/rooms.api";
import {
  createScene,
  deleteScene,
  executeScene,
  getSceneDetail,
  updateScene,
} from "@/features/scenes/scenes.api";
import {
  buildSceneSideEffectRequest,
  buildSceneTargetRequest,
  createEmptySceneTargetDraft,
  createEmptySceneSideEffectDraft,
  sceneDetailSideEffectToDraft,
  sceneDetailTargetToDraft,
  type SceneTargetDraft,
  type SceneSideEffectDraft,
} from "@/features/scenes/sceneFormUtils";
import {
  getSideEffectReadOnlyPaths,
  getTargetReadOnlyPaths,
} from "@/features/scenes/sceneReadOnlyUtils";
import { deleteHome, getHomeDevices, updateHome } from "../homes.api";
import type {
  HomeSceneBuilderDeviceDto,
} from "../homes.types";
import { useHomeDetail } from "./useHomeDetail";

type UseHomeDetailPageControllerParams = {
  homeId: string | null;
  navigateTo: (to: string) => void;
  confirmDelete: (message: string) => boolean;
  t: TFunction<"homes">;
};

type SceneModalMode = "create" | "edit";
type HomeEditMode = "name" | "description";

export function useHomeDetailPageController({
  homeId,
  navigateTo,
  confirmDelete,
  t,
}: UseHomeDetailPageControllerParams) {
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isDeleteBusy, setIsDeleteBusy] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isCreateRoomOpen, setIsCreateRoomOpen] = useState(false);
  const [isCreateDeviceOpen, setIsCreateDeviceOpen] = useState(false);
  const [newRoomName, setNewRoomName] = useState("");
  const [newRoomDescription, setNewRoomDescription] = useState("");
  const [newDeviceRoomId, setNewDeviceRoomId] = useState("");
  const [newDeviceProvisionCode, setNewDeviceProvisionCode] = useState("");
  const [roomError, setRoomError] = useState<string | null>(null);
  const [deviceError, setDeviceError] = useState<string | null>(null);
  const [homeEditMode, setHomeEditMode] = useState<HomeEditMode>("name");
  const [editName, setEditName] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [editError, setEditError] = useState<string | null>(null);
  const [isEditRoomOpen, setIsEditRoomOpen] = useState(false);
  const [editingRoomId, setEditingRoomId] = useState<string | null>(null);
  const [editRoomName, setEditRoomName] = useState("");
  const [editRoomDescription, setEditRoomDescription] = useState("");
  const [editRoomError, setEditRoomError] = useState<string | null>(null);
  const [isCreatingDevice, setIsCreatingDevice] = useState(false);

  const [executingQuickActionId, setExecutingQuickActionId] = useState<string | null>(null);
  const [deletingQuickActionId, setDeletingQuickActionId] = useState<string | null>(null);

  const [isSceneModalOpen, setIsSceneModalOpen] = useState(false);
  const [sceneModalMode, setSceneModalMode] = useState<SceneModalMode>("create");
  const [sceneEditingId, setSceneEditingId] = useState<string | null>(null);
  const [sceneName, setSceneName] = useState("");
  const [sceneDescription, setSceneDescription] = useState("");
  const [sceneIsEnabled, setSceneIsEnabled] = useState(true);
  const [sceneTargets, setSceneTargets] = useState<SceneTargetDraft[]>([]);
  const [sceneSideEffects, setSceneSideEffects] = useState<SceneSideEffectDraft[]>([]);
  const [sceneModalError, setSceneModalError] = useState<string | null>(null);
  const [isSceneSaving, setIsSceneSaving] = useState(false);

  const [sceneBuilderDevices, setSceneBuilderDevices] = useState<
    HomeSceneBuilderDeviceDto[]
  >([]);
  const [sceneBuilderDevicesByRoom, setSceneBuilderDevicesByRoom] =
    useState<Record<string, HomeSceneBuilderDeviceDto[]>>({});
  const [isSceneBuilderDevicesLoading, setIsSceneBuilderDevicesLoading] = useState(false);
  const [sceneBuilderDevicesError, setSceneBuilderDevicesError] =
    useState<string | null>(null);

  const { pushToast } = useToast();
  const capabilityRegistry = useCapabilityRegistry();
  const { home, isLoading, error, reload } = useHomeDetail(homeId);

  const canDeleteHome = Boolean(home && home.deviceCount === 0);
  const quickActions = home?.scenes ?? [];
  const sceneRooms = home?.rooms ?? [];

  const showSceneToast = useCallback(
    (messageKey: string) => {
      pushToast({
        tone: "error",
        message: t(messageKey, { defaultValue: messageKey }),
      });
    },
    [pushToast, t]
  );

  const formatCapabilityStatePreview = useCallback(
    (state: unknown) => {
      if (state === null || state === undefined) return t("notAvailable");
      if (
        typeof state === "string" ||
        typeof state === "number"
      ) {
        return String(state);
      }

      if (typeof state === "boolean") {
        return state ? t("scenes.stateOn") : t("scenes.stateOff");
      }

      if (Array.isArray(state)) {
        return state.length === 0 ? "[]" : `[${t("itemsCount", { count: state.length })}]`;
      }
      if (typeof state === "object") {
        const entries = Object.entries(state as Record<string, unknown>);
        if (entries.length === 0) return "{}";
        return entries
          .slice(0, 2)
          .map(([key, value]) => {
            if (typeof value === "boolean") {
              return `${key}: ${value ? t("scenes.stateOn") : t("scenes.stateOff")}`;
            }

            return `${key}: ${String(value)}`;
          })
          .join(" · ");
      }
      return String(state);
    },
    [t]
  );

  const openEditNameModal = useCallback(() => {
    if (!home) return;

    setHomeEditMode("name");
    setEditName(home.name);
    setEditDescription(home.description ?? "");
    setEditError(null);
    setIsEditOpen(true);
  }, [home]);

  const openEditDescriptionModal = useCallback(() => {
    if (!home) return;

    setHomeEditMode("description");
    setEditName(home.name);
    setEditDescription(home.description ?? "");
    setEditError(null);
    setIsEditOpen(true);
  }, [home]);

  const closeEditModal = useCallback(() => {
    setIsEditOpen(false);
  }, []);

  const openCreateRoomModal = useCallback(() => {
    setRoomError(null);
    setIsCreateRoomOpen(true);
  }, []);

  const closeCreateRoomModal = useCallback(() => {
    setIsCreateRoomOpen(false);
  }, []);

  const openEditRoomModal = useCallback(
    (roomId: string) => {
      const selectedRoom = home?.rooms.find((room) => room.id === roomId) ?? null;
      if (!selectedRoom) {
        return;
      }

      setEditingRoomId(roomId);
      setEditRoomName(selectedRoom.name);
      setEditRoomDescription(selectedRoom.description ?? "");
      setEditRoomError(null);
      setIsEditRoomOpen(true);
    },
    [home?.rooms]
  );

  const closeEditRoomModal = useCallback(() => {
    setIsEditRoomOpen(false);
    setEditingRoomId(null);
    setEditRoomError(null);
  }, []);

  const openCreateDeviceModal = useCallback(() => {
    if (!home) return;

    setDeviceError(null);
    setNewDeviceRoomId("");
    setNewDeviceProvisionCode("");
    setIsCreateDeviceOpen(true);
  }, [home]);

  const closeCreateDeviceModal = useCallback(() => {
    setIsCreateDeviceOpen(false);
  }, []);

  const closeSceneModal = useCallback(() => {
    setIsSceneModalOpen(false);
    setSceneEditingId(null);
    setSceneModalError(null);
  }, []);

  const loadSceneBuilderDevices = useCallback(async (): Promise<HomeSceneBuilderDeviceDto[]> => {
    if (!homeId) {
      setSceneBuilderDevices([]);
      setSceneBuilderDevicesByRoom({});
      setSceneBuilderDevicesError(null);
      setIsSceneBuilderDevicesLoading(false);
      return [];
    }

    setIsSceneBuilderDevicesLoading(true);
    setSceneBuilderDevicesError(null);

    try {
      const roomIds = (home?.rooms ?? []).map((room) => room.id);
      const [devices, roomEntries] = await Promise.all([
        getHomeDevices(homeId),
        Promise.all(
          roomIds.map(async (roomId) => {
            const roomDevices = await getHomeDevices(homeId, roomId);
            return [roomId, roomDevices] as const;
          })
        ),
      ]);

      setSceneBuilderDevices(devices);
      setSceneBuilderDevicesByRoom(Object.fromEntries(roomEntries));
      return devices;
    } catch (loadError) {
      const message =
        (loadError as Error).message || "scenes.errors.loadSceneBuilderDevicesFailed";
      setSceneBuilderDevicesError(message);
      return [];
    } finally {
      setIsSceneBuilderDevicesLoading(false);
    }
  }, [home?.rooms, homeId]);

  const handleSaveHome = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!homeId) return;

      setIsSaving(true);
      setEditError(null);

      try {
        await updateHome(homeId, {
          name: editName.trim() || undefined,
          description: editDescription.trim() || null,
        });
        setIsEditOpen(false);
        await reload(true);
      } catch (saveError) {
        setEditError((saveError as Error).message || "errors.updateHomeFailed");
      } finally {
        setIsSaving(false);
      }
    },
    [editDescription, editName, homeId, reload]
  );

  const handleDeleteHome = useCallback(async () => {
    if (!homeId || !home) return;
    if (home.deviceCount > 0) return;
    if (!confirmDelete(t("detail.deleteConfirm"))) return;

    setIsDeleteBusy(true);
    try {
      await deleteHome(homeId);
      navigateTo("/homes");
    } finally {
      setIsDeleteBusy(false);
    }
  }, [confirmDelete, home, homeId, navigateTo, t]);

  const handleCreateRoom = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!homeId || !newRoomName.trim()) {
        setRoomError("errors.roomNameRequired");
        return;
      }

      setIsSaving(true);
      setRoomError(null);

      try {
        await createRoom(homeId, {
          name: newRoomName.trim(),
          description: newRoomDescription.trim() || null,
        });
        setIsCreateRoomOpen(false);
        setNewRoomName("");
        setNewRoomDescription("");
        await reload(true);
      } catch (createError) {
        setRoomError(
          (createError as Error).message || "errors.createRoomFailed"
        );
      } finally {
        setIsSaving(false);
      }
    },
    [homeId, newRoomDescription, newRoomName, reload]
  );

  const handleSaveEditedRoom = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!homeId || !editingRoomId || !editRoomName.trim()) {
        setEditRoomError("errors.roomNameRequired");
        return;
      }

      setIsSaving(true);
      setEditRoomError(null);

      try {
        await updateRoom(homeId, editingRoomId, {
          name: editRoomName.trim(),
          description: editRoomDescription.trim() || null,
        });
        setIsEditRoomOpen(false);
        setEditingRoomId(null);
        await reload(true);
      } catch (saveError) {
        setEditRoomError((saveError as Error).message || "errors.updateRoomFailed");
      } finally {
        setIsSaving(false);
      }
    },
    [editRoomDescription, editRoomName, editingRoomId, homeId, reload]
  );

  const handleCreateDevice = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!homeId) return;

      if (!newDeviceProvisionCode.trim()) {
        setDeviceError("errors.deviceCodeRequired");
        return;
      }

      setIsCreatingDevice(true);
      setDeviceError(null);

      try {
        const normalizedRoomId = newDeviceRoomId.trim();

        await createDevice({
          homeId,
          roomId: normalizedRoomId === "" ? undefined : normalizedRoomId,
          provisionCode: newDeviceProvisionCode.trim(),
        });
        setIsCreateDeviceOpen(false);
        setNewDeviceRoomId("");
        setNewDeviceProvisionCode("");
        await reload(true);
      } catch (createError) {
        setDeviceError((createError as Error).message || "errors.createDeviceFailed");
      } finally {
        setIsCreatingDevice(false);
      }
    },
    [homeId, newDeviceRoomId, newDeviceProvisionCode, reload]
  );

  const handleExecuteQuickAction = useCallback(
    async (sceneId: string) => {
      if (!homeId) return;

      const selectedScene = quickActions.find((scene) => scene.id === sceneId);
      if (selectedScene && !selectedScene.isEnabled) {
        return;
      }

      setExecutingQuickActionId(sceneId);

      try {
        await executeScene(homeId, sceneId, {
          triggerSource: "web-ui",
        });
      } catch (executeError) {
        const message =
          (executeError as Error).message || "scenes.errors.executeFailed";
        showSceneToast(message);
      } finally {
        setExecutingQuickActionId(null);
      }
    },
    [homeId, quickActions, showSceneToast]
  );

  const openCreateQuickActionModal = useCallback(async () => {
    await loadSceneBuilderDevices();

    setSceneModalMode("create");
    setSceneEditingId(null);
    setSceneName("");
    setSceneDescription("");
    setSceneIsEnabled(true);
    setSceneTargets([]);
    setSceneSideEffects([]);
    setSceneModalError(null);
    setIsSceneModalOpen(true);
  }, [loadSceneBuilderDevices]);

  const openEditQuickActionModal = useCallback(
    async (sceneId: string) => {
      if (!homeId) return;

      setSceneModalError(null);

      try {
        const [sceneDetail, devices] = await Promise.all([
          getSceneDetail(homeId, sceneId),
          loadSceneBuilderDevices(),
        ]);

        setSceneModalMode("edit");
        setSceneEditingId(sceneId);
        setSceneName(sceneDetail.name);
        setSceneDescription(sceneDetail.description ?? "");
        setSceneIsEnabled(sceneDetail.isEnabled);
        setSceneTargets(
          sceneDetail.targets.length > 0
            ? sceneDetail.targets
              .slice()
              .sort((left, right) => left.order - right.order)
              .map(sceneDetailTargetToDraft)
              .map((targetDraft) => {
                const matchedDevice = devices.find(
                  (device) => device.id === targetDraft.deviceId
                );

                return {
                  ...targetDraft,
                  roomId: matchedDevice?.roomId ?? "",
                };
              })
            : []
        );
        setSceneSideEffects(
          sceneDetail.sideEffects.length > 0
            ? sceneDetail.sideEffects
              .slice()
              .sort((left, right) => left.order - right.order)
              .map(sceneDetailSideEffectToDraft)
              .map((sideEffectDraft) => {
                const matchedDevice = devices.find(
                  (device) => device.id === sideEffectDraft.deviceId
                );

                return {
                  ...sideEffectDraft,
                  roomId: matchedDevice?.roomId ?? "",
                };
              })
            : []
        );
        setIsSceneModalOpen(true);
      } catch (loadError) {
        const message =
          (loadError as Error).message || "scenes.errors.loadDetailFailed";
        showSceneToast(message);
      }
    },
    [homeId, loadSceneBuilderDevices, showSceneToast]
  );

  const handleChangeSceneTarget = useCallback((index: number, action: SceneTargetDraft) => {
    setSceneTargets((current) =>
      current.map((existingTarget, currentIndex) =>
        currentIndex === index ? action : existingTarget
      )
    );
  }, []);

  const handleAddSceneTarget = useCallback(() => {
    setSceneTargets((current) => [
      ...current,
      createEmptySceneTargetDraft(),
    ]);
  }, []);

  const handleRemoveSceneTarget = useCallback((index: number) => {
    setSceneTargets((current) =>
      current.filter((_, currentIndex) => currentIndex !== index)
    );
  }, []);

  const handleChangeSceneSideEffect = useCallback((index: number, sideEffect: SceneSideEffectDraft) => {
    setSceneSideEffects((current) =>
      current.map((existingSideEffect, currentIndex) =>
        currentIndex === index ? sideEffect : existingSideEffect
      )
    );
  }, []);

  const handleAddSceneSideEffect = useCallback(() => {
    setSceneSideEffects((current) => [
      ...current,
      createEmptySceneSideEffectDraft(),
    ]);
  }, []);

  const handleRemoveSceneSideEffect = useCallback((index: number) => {
    setSceneSideEffects((current) =>
      current.filter((_, currentIndex) => currentIndex !== index)
    );
  }, []);

  const handleSaveQuickAction = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!homeId) return;

      if (!sceneName.trim()) {
        setSceneModalError("scenes.errors.nameRequired");
        return;
      }

      if (sceneTargets.length === 0 && sceneSideEffects.length === 0) {
        setSceneModalError("scenes.errors.atLeastOneTargetRequired");
        return;
      }

      const targetRequests = [];
      const sideEffectRequests = [];

      for (let index = 0; index < sceneTargets.length; index += 1) {
        const parsedTarget = buildSceneTargetRequest(sceneTargets[index], {
          readOnlyPaths: getTargetReadOnlyPaths(
            sceneTargets[index],
            sceneBuilderDevices,
            capabilityRegistry.registryMap
          ),
        });

        if (!parsedTarget.value || parsedTarget.errorKey) {
          setSceneModalError(parsedTarget.errorKey ?? "scenes.errors.invalidTarget");
          return;
        }

        targetRequests.push(parsedTarget.value);
      }

      for (let index = 0; index < sceneSideEffects.length; index += 1) {
        const parsedSideEffect = buildSceneSideEffectRequest(sceneSideEffects[index], {
          readOnlyPaths: getSideEffectReadOnlyPaths(
            sceneSideEffects[index],
            sceneBuilderDevices,
            capabilityRegistry.registryMap
          ),
        });

        if (!parsedSideEffect.value || parsedSideEffect.errorKey) {
          setSceneModalError(parsedSideEffect.errorKey ?? "scenes.errors.invalidTarget");
          return;
        }

        sideEffectRequests.push(parsedSideEffect.value);
      }

      setIsSceneSaving(true);
      setSceneModalError(null);

      try {
        if (sceneModalMode === "create") {
          await createScene(homeId, {
            name: sceneName.trim(),
            description: sceneDescription.trim() || null,
            isEnabled: sceneIsEnabled,
            targets: targetRequests,
            sideEffects: sideEffectRequests,
          });
        } else {
          if (!sceneEditingId) {
            setSceneModalError("scenes.errors.notFound");
            return;
          }

          await updateScene(homeId, sceneEditingId, {
            name: sceneName.trim(),
            description: sceneDescription.trim() || null,
            isEnabled: sceneIsEnabled,
            targets: targetRequests,
            sideEffects: sideEffectRequests,
          });
        }

        setIsSceneModalOpen(false);
        await reload(true);
      } catch (saveError) {
        setSceneModalError(
          (saveError as Error).message ||
          (sceneModalMode === "create"
            ? "scenes.errors.createFailed"
            : "scenes.errors.updateFailed")
        );
      } finally {
        setIsSceneSaving(false);
      }
    },
    [
      homeId,
      reload,
      sceneTargets,
      sceneDescription,
      sceneEditingId,
      sceneIsEnabled,
      capabilityRegistry.registryMap,
      sceneBuilderDevices,
      sceneModalMode,
      sceneName,
      sceneSideEffects,
    ]
  );

  const handleDeleteQuickAction = useCallback(
    async (sceneId: string) => {
      if (!homeId) return;

      setDeletingQuickActionId(sceneId);
      setSceneModalError(null);

      try {
        await deleteScene(homeId, sceneId);
        setSceneEditingId(null);
        setIsSceneModalOpen(false);
        await reload(true);
      } catch (deleteError) {
        const message =
          (deleteError as Error).message || "scenes.errors.deleteFailed";
        setSceneModalError(message);
        showSceneToast(message);
      } finally {
        setDeletingQuickActionId(null);
      }
    },
    [homeId, reload, showSceneToast]
  );

  const handleDeleteSceneFromModal = useCallback(async () => {
    if (!sceneEditingId) {
      return;
    }

    if (!confirmDelete(t("scenes.deleteConfirm"))) {
      return;
    }

    await handleDeleteQuickAction(sceneEditingId);
  }, [confirmDelete, handleDeleteQuickAction, sceneEditingId, t]);

  return {
    canDeleteHome,
    closeCreateDeviceModal,
    closeCreateRoomModal,
    closeEditModal,
    closeEditRoomModal,
    closeSceneModal,
    deviceError,
    deletingQuickActionId,
    editDescription,
    editError,
    editHomeMode: homeEditMode,
    editName,
    editRoomDescription,
    editRoomError,
    editRoomName,
    error,
    formatCapabilityStatePreview,
    executingQuickActionId,
    handleAddSceneTarget,
    handleChangeSceneTarget,
    handleCreateDevice,
    handleCreateRoom,
    handleDeleteHome,
    handleDeleteSceneFromModal,
    handleExecuteQuickAction,
    handleSaveEditedRoom,
    handleRemoveSceneTarget,
    handleAddSceneSideEffect,
    handleChangeSceneSideEffect,
    handleRemoveSceneSideEffect,
    handleSaveHome,
    handleSaveQuickAction,
    home,
    isCreateDeviceOpen,
    isCreateRoomOpen,
    isCreatingDevice,
    isDeleteBusy,
    isEditOpen,
    isEditRoomOpen,
    isLoading,
    isSaving,
    isSceneBuilderDevicesLoading,
    isSceneDeleting: Boolean(sceneEditingId && deletingQuickActionId === sceneEditingId),
    isSceneModalOpen,
    isSceneSaving,
    roomError,
    newDeviceRoomId,
    newDeviceProvisionCode,
    newRoomDescription,
    newRoomName,
    openCreateDeviceModal,
    openCreateRoomModal,
    openCreateQuickActionModal,
    openEditDescriptionModal,
    openEditNameModal,
    openEditQuickActionModal,
    openEditRoomModal,
    quickActions,
    sceneTargets,
    sceneSideEffects,
    sceneBuilderDevices,
    sceneBuilderDevicesByRoom,
    sceneBuilderDevicesError,
    sceneDescription,
    sceneIsEnabled,
    sceneModalError,
    sceneModalMode,
    sceneName,
    sceneRooms,
    setEditDescription,
    setEditName,
    setEditRoomDescription,
    setEditRoomName,
    setNewDeviceRoomId,
    setNewDeviceProvisionCode,
    setNewRoomDescription,
    setNewRoomName,
    setSceneDescription,
    setSceneIsEnabled,
    setSceneName,
    sceneRegistryError:
      capabilityRegistry.error?.message ||
      (capabilityRegistry.error ? "scenes.errors.loadRegistryFailed" : null),
    sceneRegistryMap: capabilityRegistry.registryMap,
    sceneRegistryLoading: capabilityRegistry.isLoading,
  };
}
