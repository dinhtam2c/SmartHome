import { useCallback, useState } from "react";
import type { SyntheticEvent } from "react";
import type { TFunction } from "i18next";
import { createDevice } from "@/features/devices/devices.api";
import { deleteRoom, updateRoom } from "../rooms.api";
import { useRoomDetail } from "./useRoomDetail";

type UseRoomDetailPageControllerParams = {
  homeId: string | null;
  roomId: string | null;
  navigateTo: (to: string) => void;
  confirmDelete: (message: string) => boolean;
  t: TFunction<"rooms">;
};

type RoomEditMode = "name" | "description";

export function useRoomDetailPageController({
  homeId,
  roomId,
  navigateTo,
  confirmDelete,
  t,
}: UseRoomDetailPageControllerParams) {
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isCreateDeviceOpen, setIsCreateDeviceOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isCreatingDevice, setIsCreatingDevice] = useState(false);
  const [editMode, setEditMode] = useState<RoomEditMode>("name");
  const [editName, setEditName] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [editError, setEditError] = useState<string | null>(null);
  const [newDeviceProvisionCode, setNewDeviceProvisionCode] = useState("");
  const [createDeviceError, setCreateDeviceError] = useState<string | null>(null);

  const { room, home, isLoading, error, reload } = useRoomDetail(
    homeId,
    roomId
  );

  const canDeleteRoom = Boolean(room && room.deviceCount === 0);

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
        return state ? t("on") : t("off");
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
              return `${key}: ${value ? t("on") : t("off")}`;
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
    if (!room) return;

    setEditMode("name");
    setEditName(room.name);
    setEditDescription(room.description ?? "");
    setEditError(null);
    setIsEditOpen(true);
  }, [room]);

  const openEditDescriptionModal = useCallback(() => {
    if (!room) return;

    setEditMode("description");
    setEditName(room.name);
    setEditDescription(room.description ?? "");
    setEditError(null);
    setIsEditOpen(true);
  }, [room]);

  const closeEditModal = useCallback(() => {
    setIsEditOpen(false);
  }, []);

  const openCreateDeviceModal = useCallback(() => {
    setCreateDeviceError(null);
    setNewDeviceProvisionCode("");
    setIsCreateDeviceOpen(true);
  }, []);

  const closeCreateDeviceModal = useCallback(() => {
    setIsCreateDeviceOpen(false);
  }, []);

  const handleSaveRoom = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!homeId || !roomId) return;

      setIsSaving(true);
      setEditError(null);

      try {
        await updateRoom(homeId, roomId, {
          name: editName.trim() || undefined,
          description: editDescription.trim() || null,
        });
        setIsEditOpen(false);
        await reload(true);
      } catch (saveError) {
        setEditError((saveError as Error).message || "updateFailed");
      } finally {
        setIsSaving(false);
      }
    },
    [editDescription, editName, homeId, roomId, reload]
  );

  const handleDeleteRoom = useCallback(async () => {
    if (!homeId || !roomId || !room) return;
    if (room.deviceCount > 0) return;
    if (!confirmDelete(t("deleteConfirm"))) return;

    setIsDeleting(true);
    try {
      await deleteRoom(homeId, roomId);
      navigateTo(`/homes/${homeId}`);
    } finally {
      setIsDeleting(false);
    }
  }, [confirmDelete, homeId, room, roomId, navigateTo, t]);

  const handleCreateDevice = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!homeId || !roomId) return;

      if (!newDeviceProvisionCode.trim()) {
        setCreateDeviceError("errors.deviceCodeRequired");
        return;
      }

      setIsCreatingDevice(true);
      setCreateDeviceError(null);

      try {
        await createDevice({
          homeId,
          roomId,
          provisionCode: newDeviceProvisionCode.trim(),
        });
        setIsCreateDeviceOpen(false);
        setNewDeviceProvisionCode("");
        await reload(true);
      } catch (createError) {
        setCreateDeviceError((createError as Error).message || "errors.createDeviceFailed");
      } finally {
        setIsCreatingDevice(false);
      }
    },
    [homeId, roomId, newDeviceProvisionCode, reload]
  );

  return {
    canDeleteRoom,
    closeCreateDeviceModal,
    closeEditModal,
    createDeviceError,
    editDescription,
    editError,
    editMode,
    editName,
    error,
    formatCapabilityStatePreview,
    handleCreateDevice,
    handleDeleteRoom,
    handleSaveRoom,
    home,
    isCreateDeviceOpen,
    isCreatingDevice,
    isDeleting,
    isEditOpen,
    isLoading,
    isSaving,
    room,
    newDeviceProvisionCode,
    openCreateDeviceModal,
    openEditDescriptionModal,
    openEditNameModal,
    setNewDeviceProvisionCode,
    setEditDescription,
    setEditName,
  };
}
