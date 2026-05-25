import { useCallback, useState } from "react";
import type { SyntheticEvent } from "react";
import type { TFunction } from "i18next";
import { useToast } from "@/shared/ui/Toast";
import type { BuilderDeviceDto } from "@/features/capability-builder";
import { useCapabilityRegistry } from "@/features/capabilities";
import { createDevice } from "@/features/devices";
import { createRoom, updateRoom } from "@/features/rooms";
import {
  createScene,
  deleteScene,
  executeScene,
  getSceneDetail,
  updateScene,
} from "@/features/scenes";
import {
  actionSetDtoToDraft,
  buildActionSetRequest,
  createEmptyActionSetDraft,
  getActionReadOnlyPaths,
  type ActionDraft,
  type ActionSetDraft,
} from "@/features/action-sets";
import { deleteHome, getHomeDevices, updateHome } from "../api/homesApi";
import { useHomeDetailViewModel } from "./useHomeDetailViewModel";

type UseHomeDetailPageControllerParams = {
  homeId: string | null;
  navigateTo: (to: string) => void;
  confirmDelete: (message: string) => boolean;
  t: TFunction<"homes">;
  tScenes: TFunction<"scenes">;
};

type SceneModalMode = "create" | "edit";
type HomeEditMode = "name" | "description";

function attachRoomIdToAction(action: ActionDraft, devices: BuilderDeviceDto[]): ActionDraft {
  const matchedDevice = devices.find((device) => device.id === action.deviceId);
  return {
    ...action,
    roomId: matchedDevice?.roomId ?? "",
  };
}

function attachRoomIdsToActionSetDraft(
  draft: ActionSetDraft,
  devices: BuilderDeviceDto[]
): ActionSetDraft {
  return {
    ...draft,
    actions: draft.actions.map((action) => attachRoomIdToAction(action, devices)),
    hooks: {
      before: draft.hooks.before.map((action) => attachRoomIdToAction(action, devices)),
      onSuccess: draft.hooks.onSuccess.map((action) => attachRoomIdToAction(action, devices)),
      onFailure: draft.hooks.onFailure.map((action) => attachRoomIdToAction(action, devices)),
    },
  };
}

export function useHomeDetailPageController({
  homeId,
  navigateTo,
  confirmDelete,
  t,
  tScenes,
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
  const [sceneActionSet, setSceneActionSet] = useState<ActionSetDraft>(() =>
    createEmptyActionSetDraft()
  );
  const [sceneModalError, setSceneModalError] = useState<string | null>(null);
  const [isSceneSaving, setIsSceneSaving] = useState(false);

  const [sceneBuilderDevices, setSceneBuilderDevices] = useState<
    BuilderDeviceDto[]
  >([]);
  const [sceneBuilderDevicesByRoom, setSceneBuilderDevicesByRoom] =
    useState<Record<string, BuilderDeviceDto[]>>({});
  const [isSceneBuilderDevicesLoading, setIsSceneBuilderDevicesLoading] = useState(false);
  const [sceneBuilderDevicesError, setSceneBuilderDevicesError] =
    useState<string | null>(null);

  const { pushToast } = useToast();
  const capabilityRegistry = useCapabilityRegistry();
  const {
    canDeleteHome,
    error,
    formatCapabilityStatePreview,
    home,
    isLoading,
    quickActions,
    reload,
    sceneRooms,
  } = useHomeDetailViewModel({ homeId, t, tScenes });

  const showSceneToast = useCallback(
    (messageKey: string) => {
      pushToast({
        tone: "error",
        message: t(messageKey, { defaultValue: messageKey }),
      });
    },
    [pushToast, t]
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

  const loadSceneBuilderDevices = useCallback(async (): Promise<BuilderDeviceDto[]> => {
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
    setSceneActionSet(createEmptyActionSetDraft());
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
        setSceneActionSet(attachRoomIdsToActionSetDraft(
          actionSetDtoToDraft(sceneDetail.actionSet),
          devices
        ));
        setIsSceneModalOpen(true);
      } catch (loadError) {
        const message =
          (loadError as Error).message || "scenes.errors.loadDetailFailed";
        showSceneToast(message);
      }
    },
    [homeId, loadSceneBuilderDevices, showSceneToast]
  );

  const handleSaveQuickAction = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!homeId) return;

      if (!sceneName.trim()) {
        setSceneModalError("scenes.errors.nameRequired");
        return;
      }

      const parsedActionSet = buildActionSetRequest(sceneActionSet, {
        getReadOnlyPaths: (action) =>
          getActionReadOnlyPaths(
            action,
            sceneBuilderDevices,
            capabilityRegistry.registryMap
          ),
      });

      if (!parsedActionSet.value) {
        setSceneModalError(parsedActionSet.errorKey ?? "scenes.errors.invalidActionSet");
        return;
      }

      setIsSceneSaving(true);
      setSceneModalError(null);

      try {
        if (sceneModalMode === "create") {
          await createScene(homeId, {
            name: sceneName.trim(),
            description: sceneDescription.trim() || null,
            isEnabled: sceneIsEnabled,
            actionSet: parsedActionSet.value,
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
            actionSet: parsedActionSet.value,
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
      sceneDescription,
      sceneEditingId,
      sceneIsEnabled,
      capabilityRegistry.registryMap,
      sceneBuilderDevices,
      sceneModalMode,
      sceneName,
      sceneActionSet,
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

    if (!confirmDelete(tScenes("scenes.deleteConfirm"))) {
      return;
    }

    await handleDeleteQuickAction(sceneEditingId);
  }, [confirmDelete, handleDeleteQuickAction, sceneEditingId, tScenes]);

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
    handleCreateDevice,
    handleCreateRoom,
    handleDeleteHome,
    handleDeleteSceneFromModal,
    handleExecuteQuickAction,
    handleSaveEditedRoom,
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
    sceneActionSet,
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
    setSceneActionSet,
    sceneRegistryError:
      capabilityRegistry.error?.message ||
      (capabilityRegistry.error ? "scenes.errors.loadRegistryFailed" : null),
    sceneRegistryMap: capabilityRegistry.registryMap,
    sceneRegistryLoading: capabilityRegistry.isLoading,
  };
}
