import { useEffect, useState, useCallback } from "react";
import type { DeviceListElement, DeviceAddRequest } from "../devices.types";
import {
  addDevice as apiAddDevice,
  getDevices,
  deleteDevice as apiDeleteDevice,
} from "../devices.api";

export function useDevices() {
  const [devices, setDevices] = useState<DeviceListElement[]>([]);
  const [loading, setLoading] = useState(true);
  const [reloading, setReloading] = useState(false);
  const [isAdding, setIsAdding] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState("");

  const loadDevices = useCallback(async () => {
    try {
      const data = await getDevices();
      setDevices(data);
    } catch (err: any) {
      setError(err.message);
    }
  }, []);

  useEffect(() => {
    setLoading(true);
    loadDevices().finally(() => setLoading(false));
  }, [loadDevices]);

  const reload = useCallback(async () => {
    setReloading(true);
    await loadDevices();
    setReloading(false);
  }, [loadDevices]);

  async function addDevice(request: DeviceAddRequest) {
    try {
      setIsAdding(true);
      await apiAddDevice(request);
    } catch (err: Error | any) {
      setError(err.message);
      throw err;
    } finally {
      setIsAdding(false);
    }
  }

  async function deleteDevice(id: string) {
    try {
      setIsDeleting(true);
      await apiDeleteDevice(id);
      await reload();
    } catch (err: Error | any) {
      setError(err.message);
      throw err;
    } finally {
      setIsDeleting(false);
    }
  }

  return {
    devices,
    loading,
    reloading,
    isAdding,
    isDeleting,
    error,
    addDevice,
    deleteDevice,
    reload,
  };
}
