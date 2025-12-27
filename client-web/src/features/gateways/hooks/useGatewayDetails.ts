import { useEffect, useState } from "react";
import type {
  GatewayDetails,
  GatewayHomeAssignRequest,
} from "../gateways.types";
import {
  getGatewayDetails,
  assignHomeToGateway as apiAssignHome,
} from "../gateways.api";

export function useGatewayDetails(id: string | null) {
  const [gateway, setGateway] = useState<GatewayDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [isAssigning, setIsAssigning] = useState(false);

  useEffect(() => {
    loadGateway();
  }, [id]);

  async function loadGateway() {
    if (!id) return;
    setLoading(true);
    try {
      const data = await getGatewayDetails(id);
      setGateway(data);
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function assignHome(request: GatewayHomeAssignRequest) {
    if (!gateway) return;
    try {
      setIsAssigning(true);
      await apiAssignHome(gateway.id, request);
      await loadGateway();
    } catch (err: Error | any) {
      setError(err.message);
    } finally {
      setIsAssigning(false);
    }
  }

  return {
    gateway,
    loading,
    error,
    isAssigning,
    assignHome,
  };
}
