import { useEffect, useState, useCallback } from "react";
import type {
  LocationListElement,
  LocationAddRequest,
} from "../locations.types";
import {
  addLocation as apiAddLocation,
  deleteLocation as apiDeleteLocation,
  getLocations,
} from "../locations.api";

export function useLocations(homeId: string | null) {
  const [locations, setLocations] = useState<LocationListElement[]>([]);
  const [loading, setLoading] = useState(true);
  const [reloading, setReloading] = useState(false);
  const [isAdding, setIsAdding] = useState(false);
  const [error, setError] = useState("");

  const loadLocations = useCallback(async () => {
    if (!homeId) return;
    try {
      const data = await getLocations(homeId);
      setLocations(data);
    } catch (err: any) {
      setError(err.message);
    }
  }, [homeId]);

  useEffect(() => {
    setLoading(true);
    loadLocations().finally(() => setLoading(false));
  }, [loadLocations]);

  const reload = useCallback(async () => {
    setReloading(true);
    await loadLocations();
    setReloading(false);
  }, [loadLocations]);

  async function addLocation(request: LocationAddRequest) {
    try {
      setIsAdding(true);
      await apiAddLocation(request);
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsAdding(false);
    }
  }

  async function deleteLocation(id: string) {
    try {
      await apiDeleteLocation(id);
    } catch (err: Error | any) {
      setError(err.message);
    }
  }

  return {
    locations,
    loading,
    reloading,
    isAdding,
    error,
    addLocation,
    deleteLocation,
    reload,
  };
}
