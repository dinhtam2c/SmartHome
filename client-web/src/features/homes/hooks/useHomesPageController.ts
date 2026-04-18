import { useCallback, useMemo, useState } from "react";
import type { SyntheticEvent } from "react";
import { createHome, updateHome } from "../homes.api";
import { useHomes } from "./useHomes";

export function useHomesPageController() {
  const [query, setQuery] = useState("");
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [editError, setEditError] = useState<string | null>(null);
  const [editingHomeId, setEditingHomeId] = useState<string | null>(null);
  const [newName, setNewName] = useState("");
  const [newDescription, setNewDescription] = useState("");
  const [editName, setEditName] = useState("");
  const [editDescription, setEditDescription] = useState("");

  const { homes, isLoading, error, reload } = useHomes();

  const filteredHomes = useMemo(
    () =>
      homes.filter((home) =>
        home.name.toLowerCase().includes(query.toLowerCase())
      ),
    [homes, query]
  );

  const openCreateModal = useCallback(() => {
    setCreateError(null);
    setIsCreateOpen(true);
  }, []);

  const closeCreateModal = useCallback(() => {
    setIsCreateOpen(false);
  }, []);

  const openEditModal = useCallback(
    (homeId: string) => {
      const selectedHome = homes.find((home) => home.id === homeId) ?? null;
      if (!selectedHome) {
        return;
      }

      setEditingHomeId(homeId);
      setEditName(selectedHome.name);
      setEditDescription(selectedHome.description ?? "");
      setEditError(null);
      setIsEditOpen(true);
    },
    [homes]
  );

  const closeEditModal = useCallback(() => {
    setIsEditOpen(false);
    setEditingHomeId(null);
    setEditError(null);
  }, []);

  const handleCreateHome = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!newName.trim()) {
        setCreateError("errors.homeNameRequired");
        return;
      }

      setIsCreating(true);
      setCreateError(null);

      try {
        await createHome({
          name: newName.trim(),
          description: newDescription.trim(),
        });
        setIsCreateOpen(false);
        setNewName("");
        setNewDescription("");
        await reload(true);
      } catch (error) {
        setCreateError((error as Error).message || "errors.createHomeFailed");
      } finally {
        setIsCreating(false);
      }
    },
    [newDescription, newName, reload]
  );

  const handleSaveHome = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!editingHomeId) {
        return;
      }

      if (!editName.trim()) {
        setEditError("errors.homeNameRequired");
        return;
      }

      setIsEditing(true);
      setEditError(null);

      try {
        await updateHome(editingHomeId, {
          name: editName.trim(),
          description: editDescription.trim() || null,
        });
        setIsEditOpen(false);
        setEditingHomeId(null);
        await reload(true);
      } catch (error) {
        setEditError((error as Error).message || "errors.updateHomeFailed");
      } finally {
        setIsEditing(false);
      }
    },
    [editDescription, editName, editingHomeId, reload]
  );

  return {
    createError,
    editDescription,
    editError,
    editName,
    error,
    filteredHomes,
    handleCreateHome,
    handleSaveHome,
    homes,
    isCreateOpen,
    isEditOpen,
    isCreating,
    isEditing,
    isLoading,
    newDescription,
    newName,
    openEditModal,
    openCreateModal,
    closeEditModal,
    closeCreateModal,
    query,
    setEditDescription,
    setEditName,
    setNewDescription,
    setNewName,
    setQuery,
  };
}
