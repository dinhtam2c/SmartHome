import { useEffect, useState } from "react";
import { getDeviceCategoriesCached } from "../api/deviceCategoriesApi";
import type { DeviceCategoryDefinition } from "../types/deviceCategoryTypes";
import { FALLBACK_DEVICE_CATEGORIES } from "../services/deviceCategoryPresentationService";

export function useDeviceCategoryRegistry() {
  const [categories, setCategories] = useState<DeviceCategoryDefinition[]>(
    FALLBACK_DEVICE_CATEGORIES
  );
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    let cancelled = false;

    getDeviceCategoriesCached()
      .then((nextCategories) => {
        if (!cancelled) {
          setCategories(nextCategories);
          setError(null);
        }
      })
      .catch((nextError) => {
        if (!cancelled) {
          setCategories(FALLBACK_DEVICE_CATEGORIES);
          setError(nextError as Error);
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return {
    categories,
    error,
  };
}
