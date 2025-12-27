import { useEffect, useState } from "react";
import type {
  LocationDetails,
  LocationUpdateRequest,
} from "../locations.types";
import {
  getLocationDetails,
  updateLocation as apiUpdateLocation,
  deleteLocation as apiDeleteLocation,
} from "../locations.api";

export function useLocationDetails(id: string | null) {
  const [location, setLocation] = useState<LocationDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [isUpdating, setIsUpdating] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    loadLocation();
  }, [id]);

  async function loadLocation() {
    if (!id) return;
    setLoading(true);
    try {
      const data = await getLocationDetails(id);
      setLocation(data);
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function updateLocation(request: LocationUpdateRequest) {
    if (!location) return;
    try {
      setIsUpdating(true);
      await apiUpdateLocation(location.id, request);
      await loadLocation();
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsUpdating(false);
    }
  }

  async function deleteLocation() {
    if (!location) return;
    try {
      setIsDeleting(true);
      await apiDeleteLocation(location.id);
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsDeleting(false);
    }
  }

  return {
    location,
    loading,
    error,
    isUpdating,
    isDeleting,
    updateLocation,
    deleteLocation,
  };
}
