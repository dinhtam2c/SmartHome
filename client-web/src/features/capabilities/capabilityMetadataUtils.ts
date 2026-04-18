import type { CapabilityRegistryMetadata } from "./capabilities.types";

const PATH_ARRAY_INDEX_PATTERN = /^\d+$/;

type PrimaryOperationReference = {
  operation: string | null;
  valuePath: string | null;
};

function toPathSegments(path: string): string[] {
  return path
    .replace(/\[(\d+)\]/g, ".$1")
    .split(".")
    .map((segment) => segment.trim())
    .filter((segment) => segment !== "");
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function parsePrimaryOperationReference(
  operationReference: string | null | undefined
): PrimaryOperationReference {
  const normalized = operationReference?.trim();

  if (!normalized) {
    return {
      operation: null,
      valuePath: null,
    };
  }

  const separatorIndex = normalized.indexOf(".");
  if (separatorIndex < 0) {
    return {
      operation: normalized,
      valuePath: null,
    };
  }

  const operation = normalized.slice(0, separatorIndex).trim();
  const valuePath = normalized.slice(separatorIndex + 1).trim();

  return {
    operation: operation || null,
    valuePath: valuePath || null,
  };
}

export function getValueByPath(source: unknown, path: string | null | undefined): unknown {
  const normalizedPath = path?.trim();

  if (!normalizedPath) {
    return source;
  }

  const segments = toPathSegments(normalizedPath);
  if (segments.length === 0) {
    return source;
  }

  let current: unknown = source;

  for (const segment of segments) {
    if (Array.isArray(current)) {
      if (!PATH_ARRAY_INDEX_PATTERN.test(segment)) {
        return undefined;
      }

      current = current[Number(segment)];
      continue;
    }

    if (isRecord(current)) {
      current = current[segment];
      continue;
    }

    return undefined;
  }

  return current;
}

export function composeValueByPath(
  valuePath: string | null | undefined,
  value: unknown
): unknown {
  const normalizedPath = valuePath?.trim();

  if (!normalizedPath) {
    return value;
  }

  const segments = toPathSegments(normalizedPath);
  if (segments.length === 0) {
    return value;
  }

  const firstIsArrayIndex = PATH_ARRAY_INDEX_PATTERN.test(segments[0]);
  const root: Record<string, unknown> | unknown[] = firstIsArrayIndex ? [] : {};

  let current: Record<string, unknown> | unknown[] = root;

  segments.forEach((segment, index) => {
    const isLast = index === segments.length - 1;

    if (Array.isArray(current)) {
      const arrayIndex = Number(segment);

      if (!Number.isInteger(arrayIndex) || arrayIndex < 0) {
        return;
      }

      if (isLast) {
        current[arrayIndex] = value;
        return;
      }

      const nextIsArrayIndex = PATH_ARRAY_INDEX_PATTERN.test(segments[index + 1]);
      const nextContainer: Record<string, unknown> | unknown[] = nextIsArrayIndex ? [] : {};
      current[arrayIndex] = nextContainer;
      current = nextContainer;
      return;
    }

    if (isLast) {
      current[segment] = value;
      return;
    }

    const nextIsArrayIndex = PATH_ARRAY_INDEX_PATTERN.test(segments[index + 1]);
    const nextContainer: Record<string, unknown> | unknown[] = nextIsArrayIndex ? [] : {};
    current[segment] = nextContainer;
    current = nextContainer;
  });

  return root;
}

export function getCapabilityPrimaryStateValue(
  state: unknown,
  primaryStatePath: string | null | undefined
): unknown {
  if (state === null || state === undefined) {
    return state;
  }

  const byPrimaryPath = getValueByPath(state, primaryStatePath);
  if (byPrimaryPath !== undefined) {
    return byPrimaryPath;
  }

  if (isRecord(state)) {
    if ("value" in state) {
      return state.value;
    }

    const values = Object.values(state);
    if (values.length === 0) {
      return undefined;
    }

    const firstValue = values[0];
    if (isRecord(firstValue) && "value" in firstValue) {
      return firstValue.value;
    }

    return firstValue;
  }

  return state;
}

export function isCapabilityOverviewVisible(
  metadata: CapabilityRegistryMetadata | null | undefined
) {
  return metadata?.overviewVisible !== false;
}

export function getCapabilityDisplayOrder(
  metadata: CapabilityRegistryMetadata | null | undefined
) {
  const order = metadata?.order;

  if (typeof order === "number" && Number.isFinite(order)) {
    return order;
  }

  return Number.POSITIVE_INFINITY;
}

export function getCapabilityUnit(metadata: CapabilityRegistryMetadata | null | undefined) {
  if (typeof metadata?.unit === "string" && metadata.unit.trim() !== "") {
    return metadata.unit.trim();
  }

  return null;
}
