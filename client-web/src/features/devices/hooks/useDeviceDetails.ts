import { useEffect, useState } from "react";
import type {
  DeviceDetails,
  DeviceLocationAssignRequest,
  DeviceGatewayAssignRequest,
  DeviceUpdateRequest,
} from "../devices.types";
import {
  getDeviceDetails,
  assignLocationToDevice as apiAssignLocation,
  assignGatewayToDevice as apiAssignGateway,
  updateDevice as apiUpdateDevice,
} from "../devices.api";

export function useDeviceDetails(id: string | null) {
  const [device, setDevice] = useState<DeviceDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [isAssigningLocation, setIsAssigningLocation] = useState(false);
  const [isAssigningGateway, setIsAssigningGateway] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);

  useEffect(() => {
    loadDevice();
  }, [id]);

  async function loadDevice() {
    if (!id) return;
    setLoading(true);
    try {
      const data = await getDeviceDetails(id);
      setDevice(data);
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function assignLocation(request: DeviceLocationAssignRequest) {
    if (!device) return;
    try {
      setIsAssigningLocation(true);
      await apiAssignLocation(device.id, request);
      await loadDevice();
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsAssigningLocation(false);
    }
  }

  async function assignGateway(request: DeviceGatewayAssignRequest) {
    if (!device) return;
    try {
      setIsAssigningGateway(true);
      await apiAssignGateway(device.id, request);
      await loadDevice();
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsAssigningGateway(false);
    }
  }

  async function updateDevice(request: DeviceUpdateRequest) {
    if (!device) return;
    try {
      setIsUpdating(true);
      await apiUpdateDevice(device.id, request);
      await loadDevice();
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsUpdating(false);
    }
  }

  return {
    device,
    loading,
    error,
    isAssigningLocation,
    isAssigningGateway,
    isUpdating,
    assignLocation,
    assignGateway,
    updateDevice,
  };
}
