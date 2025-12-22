import { useEffect, useState } from "react";
import type { HomeDetails, HomeUpdateRequest } from "../homes.types";
import {
  getHomeDetails,
  updateHome as apiUpdateHome,
  deleteHome as apiDeleteHome,
} from "../homes.api";

export function useHomeDetails(id: string | null) {
  const [home, setHome] = useState<HomeDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [isUpdating, setIsUpdating] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);


  useEffect(() => {
    loadHome();
  }, [id]);

  async function loadHome() {
    if (!id) return;
    setLoading(true);
    try {
      const data = await getHomeDetails(id);
      setHome(data);
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function updateHome(request: HomeUpdateRequest) {
    if (!home) return;
    try {
      setIsUpdating(true);
      await apiUpdateHome(home.id, request);
      await loadHome();
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsUpdating(false);
    }
  }

  async function deleteHome() {
    if (!home) return;
    try {
      setIsDeleting(true);
      await apiDeleteHome(home.id);
      setHome(null);
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsDeleting(false);
    }
  }

  return { home, loading, error, isUpdating, isDeleting, updateHome, deleteHome, reload: loadHome };
}
