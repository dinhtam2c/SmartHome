import { useEffect, useState, useCallback } from "react";
import type { GatewayListElement } from "../gateways.types";
import { getGateways } from "../gateways.api";

export function useGateways() {
  const [gateways, setGateways] = useState<GatewayListElement[]>([]);
  const [loading, setLoading] = useState(true);
  const [reloading, setReloading] = useState(false);
  const [error, setError] = useState("");

  const loadGateways = useCallback(async () => {
    try {
      const data = await getGateways();
      setGateways(data);
    } catch (err: any) {
      setError(err.message);
    }
  }, []);

  useEffect(() => {
    setLoading(true);
    loadGateways().finally(() => setLoading(false));
  }, [loadGateways]);

  const reload = useCallback(async () => {
    setReloading(true);
    await loadGateways();
    setReloading(false);
  }, [loadGateways]);

  return {
    gateways,
    loading,
    reloading,
    error,
    reload,
  };
}
