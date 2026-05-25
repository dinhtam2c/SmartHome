import type { DeviceCategoryDefinition } from "../types/deviceCategoryTypes";

const OTHER_DEVICE_CATEGORY_ID = "other";

export const FALLBACK_DEVICE_CATEGORIES: DeviceCategoryDefinition[] = [
  {
    id: "light",
    defaultName: "Light",
    iconKey: "lightbulb",
    color: "#f2c264",
    order: 10,
  },
  {
    id: "fan",
    defaultName: "Fan",
    iconKey: "fan",
    color: "#65b7a7",
    order: 20,
  },
  {
    id: "sensor",
    defaultName: "Sensor",
    iconKey: "sensor",
    color: "#68b8e8",
    order: 30,
  },
  {
    id: "door",
    defaultName: "Door",
    iconKey: "door",
    color: "#72b99b",
    order: 40,
  },
  {
    id: "switch",
    defaultName: "Switch",
    iconKey: "switch",
    color: "#95acd6",
    order: 50,
  },
  {
    id: "alarm",
    defaultName: "Alarm",
    iconKey: "bell",
    color: "#df8176",
    order: 60,
  },
  {
    id: OTHER_DEVICE_CATEGORY_ID,
    defaultName: "Other device",
    iconKey: "box",
    color: "#9fb0bd",
    order: 999,
  },
];

export type DeviceCategoryIconPath = {
  d: string;
  fill?: boolean;
};

export const DEVICE_CATEGORY_ICON_PATHS: Record<string, DeviceCategoryIconPath[]> = {
  lightbulb: [
    { d: "M9 21h6" },
    { d: "M10 18h4" },
    { d: "M8 11a4 4 0 1 1 8 0c0 2-2 3-2 5h-4c0-2-2-3-2-5Z" },
    { d: "M12 2v2" },
    { d: "M4.9 4.9l1.4 1.4" },
    { d: "M19.1 4.9l-1.4 1.4" },
  ],
  fan: [
    { d: "M12 12m-2 0a2 2 0 1 0 4 0a2 2 0 1 0-4 0" },
    { d: "M12 10c1-5 7-5 7-1 0 3-3 4-5 3" },
    { d: "M13.7 13c4 3 1 8-2 6-2.6-1.5-1.8-4.6 0-6" },
    { d: "M10.3 13c-5 2-8-3-5-6 2-2 5-.7 6 2.6" },
  ],
  sensor: [
    { d: "M12 12m-2.5 0a2.5 2.5 0 1 0 5 0a2.5 2.5 0 1 0-5 0" },
    { d: "M5 12a7 7 0 0 1 14 0" },
    { d: "M2.5 12a9.5 9.5 0 0 1 19 0" },
    { d: "M7 17a7 7 0 0 0 10 0" },
  ],
  door: [
    { d: "M7 21V4.5L17 3v18" },
    { d: "M7 21h12" },
    { d: "M14 12h.1" },
  ],
  switch: [
    { d: "M8 7h8a5 5 0 0 1 0 10H8A5 5 0 0 1 8 7Z" },
    { d: "M8 12m-2 0a2 2 0 1 0 4 0a2 2 0 1 0-4 0" },
  ],
  bell: [
    { d: "M18 16v-5a6 6 0 0 0-12 0v5l-2 2h16Z" },
    { d: "M10 20a2 2 0 0 0 4 0" },
  ],
  box: [
    { d: "M4 8l8-4 8 4-8 4Z" },
    { d: "M4 8v8l8 4 8-4V8" },
    { d: "M12 12v8" },
  ],
};

function normalizeDeviceCategoryId(category: string | null | undefined) {
  const normalized = category?.trim().toLowerCase();
  return normalized || OTHER_DEVICE_CATEGORY_ID;
}

export function resolveDeviceCategory(
  categories: DeviceCategoryDefinition[],
  categoryId: string | null | undefined
) {
  const normalizedCategoryId = normalizeDeviceCategoryId(categoryId);
  const category =
    categories.find((entry) => entry.id === normalizedCategoryId) ??
    FALLBACK_DEVICE_CATEGORIES.find((entry) => entry.id === normalizedCategoryId) ??
    FALLBACK_DEVICE_CATEGORIES.find((entry) => entry.id === OTHER_DEVICE_CATEGORY_ID);

  return category ?? FALLBACK_DEVICE_CATEGORIES[FALLBACK_DEVICE_CATEGORIES.length - 1];
}

export function getDeviceCategoryLabel(
  category: DeviceCategoryDefinition,
  translate: (key: string, fallback: string) => string
) {
  return translate(`deviceCategories.${category.id}`, category.defaultName);
}
