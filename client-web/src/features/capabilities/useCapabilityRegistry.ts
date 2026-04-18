import { useEffect, useMemo, useState } from "react";
import {
  buildCapabilityRegistryMap,
  getCapabilityRegistryCached,
} from "./capabilities.api";
import type { CapabilityRegistryEntry } from "./capabilities.types";

export function useCapabilityRegistry() {
  const [entries, setEntries] = useState<CapabilityRegistryEntry[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    let isActive = true;

    async function loadRegistry() {
      setIsLoading(true);
      setError(null);

      try {
        const nextEntries = await getCapabilityRegistryCached();
        if (!isActive) {
          return;
        }

        setEntries(nextEntries);
      } catch (loadError) {
        if (!isActive) {
          return;
        }

        setError(loadError as Error);
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    void loadRegistry();

    return () => {
      isActive = false;
    };
  }, []);

  const registryMap = useMemo(() => buildCapabilityRegistryMap(entries), [entries]);

  return {
    entries,
    registryMap,
    isLoading,
    error,
  };
}
