import { api } from "@/services/http";
import type {
  CapabilityRegistryApplyStrategy,
  CapabilityRegistryEntry,
  CapabilityRegistryMetadata,
  CapabilityRegistryPrerequisite,
  CapabilityRegistryMap,
} from "./capabilities.types";

const basePath = "/capabilities";

let registryCache: CapabilityRegistryEntry[] | null = null;
let registryPromise: Promise<CapabilityRegistryEntry[]> | null = null;

export function getCapabilityRegistryKey(capabilityId: string, version: number) {
  return `${capabilityId.trim().toLowerCase()}@${version}`;
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
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

  return {
    ...metadata,
    defaultName:
      typeof metadata.defaultName === "string" && metadata.defaultName.trim() !== ""
        ? metadata.defaultName.trim()
        : undefined,
    primary,
    unit:
      typeof metadata.unit === "string" && metadata.unit.trim() !== ""
        ? metadata.unit.trim()
        : undefined,
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
  entry: CapabilityRegistryEntry
): CapabilityRegistryEntry {
  return {
    ...entry,
    id: entry.id.trim(),
    operations:
      entry.operations && isPlainObject(entry.operations)
        ? entry.operations
        : {},
    stateSchema: isPlainObject(entry.stateSchema) ? entry.stateSchema : {},
    metadata: normalizeMetadata(entry.metadata),
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

export async function fetchCapabilityRegistry() {
  const entries = await api<CapabilityRegistryEntry[]>(`${basePath}/registry`);
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
