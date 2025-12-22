import { useEffect, useState, useCallback } from "react";
import type { HomeListElement, HomeAddRequest } from "../homes.types";
import {
  addHome as apiAddHome,
  deleteHome as apiDeleteHome,
  getHomes,
} from "../homes.api";

export function useHomes() {
  const [homes, setHomes] = useState<HomeListElement[]>([]);
  const [loading, setLoading] = useState(true);
  const [reloading, setReloading] = useState(false);
  const [isAdding, setIsAdding] = useState(false);
  const [error, setError] = useState("");

  const loadHomes = useCallback(async () => {
    try {
      const data = await getHomes();
      setHomes(data);
    } catch (err: any) {
      setError(err.message);
    }
  }, []);

  useEffect(() => {
    setLoading(true);
    loadHomes().finally(() => setLoading(false));
  }, [loadHomes]);

  const reload = useCallback(async () => {
    setReloading(true);
    await loadHomes();
    setReloading(false);
  }, [loadHomes]);

  async function addHome(request: HomeAddRequest) {
    try {
      setIsAdding(true);
      await apiAddHome(request);
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsAdding(false);
    }
  }

  async function deleteHome(id: string) {
    try {
      await apiDeleteHome(id);
    } catch (err: Error | any) {
      setError(err.message);
    }
  }

  return { homes, loading, reloading, isAdding, error, addHome, deleteHome, reload };
}
