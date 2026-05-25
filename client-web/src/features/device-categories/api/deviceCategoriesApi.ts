import { api } from "@/shared/api/http";
import type { DeviceCategoryDefinition } from "../types/deviceCategoryTypes";
import { FALLBACK_DEVICE_CATEGORIES } from "../services/deviceCategoryPresentationService";

const basePath = "/device-categories";

let cachedDeviceCategories: Promise<DeviceCategoryDefinition[]> | null = null;

function normalizeDeviceCategories(categories: DeviceCategoryDefinition[]) {
  const normalized = categories
    .map((category) => ({
      id: category.id.trim().toLowerCase(),
      defaultName: category.defaultName.trim(),
      iconKey: category.iconKey.trim(),
      color: category.color.trim(),
      order: category.order,
    }))
    .filter((category) => category.id && category.defaultName && category.iconKey);

  return normalized.length > 0 ? normalized : FALLBACK_DEVICE_CATEGORIES;
}

function getDeviceCategories() {
  return api<DeviceCategoryDefinition[]>(basePath).then(normalizeDeviceCategories);
}

export function getDeviceCategoriesCached() {
  cachedDeviceCategories ??= getDeviceCategories().catch((error) => {
    cachedDeviceCategories = null;
    throw error;
  });

  return cachedDeviceCategories;
}
