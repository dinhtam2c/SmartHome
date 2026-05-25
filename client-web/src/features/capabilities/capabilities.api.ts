import { api } from "@/shared/api/http";
import { isPlainObject } from "@/shared/lib/objectUtils";
import type {
  CapabilityRegistryApplyStrategy,
  CapabilityControlCommitPolicy,
  CapabilityControlMetadata,
  CapabilityRegistryEntry,
  CapabilityRegistryMetadata,
  CapabilityRegistryPrerequisite,
  CapabilityRegistryMap,
  CapabilityVisualIcon,
  CapabilityVisualMetadata,
  CapabilityVisualTone,
} from "./capabilities.types";
import { getCapabilityMetadata } from "./capabilityMetadataRegistry";

const basePath = "/capabilities";

let registryCache: CapabilityRegistryEntry[] | null = null;
let registryPromise: Promise<CapabilityRegistryEntry[]> | null = null;

type CapabilityRegistryApiEntry = Omit<CapabilityRegistryEntry, "metadata"> & {
  metadata?: unknown;
};

export function getCapabilityRegistryKey(capabilityId: string, version: number) {
  return `${capabilityId.trim().toLowerCase()}@${version}`;
}

function normalizeStringArray(value: unknown) {
  if (!Array.isArray(value)) {
    return [];
  }

  return value
    .filter((item): item is string => typeof item === "string")
    .map((item) => item.trim())
    .filter((item) => item !== "");
}

const CAPABILITY_VISUAL_ICONS = new Set<CapabilityVisualIcon>([
  "brightness",
  "buzzer",
  "effect",
  "fan",
  "humidity",
  "illuminance",
  "light",
  "lock",
  "motion",
  "palette",
  "power",
  "temperature",
  "timer",
]);

const CAPABILITY_VISUAL_TONES = new Set<CapabilityVisualTone>([
  "amber",
  "blue",
  "green",
  "neutral",
  "red",
  "violet",
]);

const CAPABILITY_CONTROL_COMMIT_POLICIES = new Set<CapabilityControlCommitPolicy>([
  "commitOnRelease",
  "formOnly",
  "immediate",
  "liveThrottle",
]);

function normalizeVisualMetadata(value: unknown): CapabilityVisualMetadata | undefined {
  if (!isPlainObject(value)) {
    return undefined;
  }

  const metadata: CapabilityVisualMetadata = {};

  if (typeof value.icon === "string" && CAPABILITY_VISUAL_ICONS.has(value.icon as CapabilityVisualIcon)) {
    metadata.icon = value.icon as CapabilityVisualIcon;
  }

  if (typeof value.image === "string" && value.image.trim() !== "") {
    metadata.image = value.image.trim();
  }

  if (typeof value.precision === "number" && Number.isInteger(value.precision)) {
    metadata.precision = Math.max(0, Math.min(value.precision, 6));
  }

  if (typeof value.tone === "string" && CAPABILITY_VISUAL_TONES.has(value.tone as CapabilityVisualTone)) {
    metadata.tone = value.tone as CapabilityVisualTone;
  }

  return Object.keys(metadata).length > 0 ? metadata : undefined;
}

function normalizeControlMetadata(value: unknown): CapabilityControlMetadata | undefined {
  if (!isPlainObject(value)) {
    return undefined;
  }

  const metadata: CapabilityControlMetadata = {};

  if (
    typeof value.commitPolicy === "string" &&
    CAPABILITY_CONTROL_COMMIT_POLICIES.has(value.commitPolicy as CapabilityControlCommitPolicy)
  ) {
    metadata.commitPolicy = value.commitPolicy as CapabilityControlCommitPolicy;
  }

  if (typeof value.throttleMs === "number" && Number.isFinite(value.throttleMs)) {
    metadata.throttleMs = Math.max(0, Math.min(Math.trunc(value.throttleMs), 5000));
  }

  return Object.keys(metadata).length > 0 ? metadata : undefined;
}

function normalizePrerequisite(value: unknown): CapabilityRegistryPrerequisite | null {
  if (!isPlainObject(value)) {
    return null;
  }

  const capabilityId =
    typeof value.capabilityId === "string" && value.capabilityId.trim() !== ""
      ? value.capabilityId.trim()
      : null;

  if (!capabilityId) {
    return null;
  }

  const requiredState = isPlainObject(value.requiredState)
    ? value.requiredState
    : {};

  return {
    capabilityId,
    requiredState,
    autoFix: typeof value.autoFix === "boolean" ? value.autoFix : undefined,
  };
}

function normalizeApplyStrategy(value: unknown): CapabilityRegistryApplyStrategy | null {
  if (!isPlainObject(value)) {
    return null;
  }

  const operation =
    typeof value.operation === "string" && value.operation.trim() !== ""
      ? value.operation.trim()
      : null;

  if (!operation || !isPlainObject(value.stateMapping)) {
    return null;
  }

  const stateMappingEntries = Object.entries(value.stateMapping)
    .filter(
      (entry): entry is [string, string] =>
        entry[0].trim() !== "" && typeof entry[1] === "string" && entry[1].trim() !== ""
    )
    .map(([key, target]) => [key.trim(), target.trim()] as const);

  if (stateMappingEntries.length === 0) {
    return null;
  }

  return {
    operation,
    stateMapping: Object.fromEntries(stateMappingEntries),
    readOnlyFields: normalizeStringArray(value.readOnlyFields),
    partialUpdate:
      typeof value.partialUpdate === "boolean" ? value.partialUpdate : undefined,
  };
}

function normalizeMetadata(metadata: unknown): CapabilityRegistryMetadata {
  if (!isPlainObject(metadata)) {
    return {};
  }

  const primary = isPlainObject(metadata.primary) ? metadata.primary : undefined;
  const visual = normalizeVisualMetadata(metadata.visual);
  const control = normalizeControlMetadata(metadata.control);

  return {
    ...metadata,
    control,
    defaultName:
      typeof metadata.defaultName === "string" && metadata.defaultName.trim() !== ""
        ? metadata.defaultName.trim()
        : undefined,
    primary,
    unit:
      typeof metadata.unit === "string" && metadata.unit.trim() !== ""
        ? metadata.unit.trim()
        : undefined,
    visual,
    overviewVisible:
      typeof metadata.overviewVisible === "boolean"
        ? metadata.overviewVisible
        : undefined,
    order:
      typeof metadata.order === "number" && Number.isFinite(metadata.order)
        ? metadata.order
        : undefined,
  };
}

function normalizeRegistryEntry(
  entry: CapabilityRegistryApiEntry
): CapabilityRegistryEntry {
  const id = entry.id.trim();

  return {
    ...entry,
    id,
    operations:
      entry.operations && isPlainObject(entry.operations)
        ? entry.operations
        : {},
    stateSchema: isPlainObject(entry.stateSchema) ? entry.stateSchema : {},
    metadata: normalizeMetadata(getCapabilityMetadata(id, entry.version)),
    conflictsWith: normalizeStringArray(entry.conflictsWith),
    prerequisite: normalizePrerequisite(entry.prerequisite),
    applyStrategy: normalizeApplyStrategy(entry.applyStrategy),
  };
}

export function buildCapabilityRegistryMap(
  entries: CapabilityRegistryEntry[]
): CapabilityRegistryMap {
  const map: CapabilityRegistryMap = new Map();

  entries.forEach((entry) => {
    map.set(getCapabilityRegistryKey(entry.id, entry.version), entry);
  });

  return map;
}

async function fetchCapabilityRegistry() {
  const entries = await api<CapabilityRegistryApiEntry[]>(`${basePath}/registry`);
  return entries.map(normalizeRegistryEntry);
}

export async function getCapabilityRegistryCached(forceRefresh = false) {
  if (forceRefresh) {
    registryCache = null;
    registryPromise = null;
  }

  if (registryCache) {
    return registryCache;
  }

  if (!registryPromise) {
    registryPromise = fetchCapabilityRegistry()
      .then((entries) => {
        registryCache = entries;
        return entries;
      })
      .finally(() => {
        registryPromise = null;
      });
  }

  return registryPromise;
}
