import {
  composeValueByPath,
  getValueByPath,
} from "@/features/capabilities";
import { isPlainObject, mergeRecords } from "@/shared/lib/objectUtils";

export type RequiredJsonObjectErrorKeys = {
  required: string;
  invalidJson: string;
  mustBeObject: string;
};

function toPathSegments(path: string): string[] {
  return path
    .replace(/\[(\d+)\]/g, ".$1")
    .split(".")
    .map((segment) => segment.trim())
    .filter((segment) => segment !== "");
}

function isArrayIndexSegment(segment: string) {
  return /^\d+$/.test(segment);
}

export function parseJsonObjectLoose(jsonText: string): Record<string, unknown> {
  const trimmed = jsonText.trim();

  if (!trimmed) {
    return {};
  }

  try {
    const parsed = JSON.parse(trimmed) as unknown;

    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      return {};
    }

    return parsed as Record<string, unknown>;
  } catch {
    return {};
  }
}

export function getJsonObjectFieldValue(
  jsonText: string,
  path: string
): unknown {
  const stateObject = parseJsonObjectLoose(jsonText);
  return getValueByPath(stateObject, path);
}

export function updateJsonObjectFieldValue(
  jsonText: string,
  path: string,
  value: unknown
) {
  const normalizedPath = path.trim();

  if (!normalizedPath) {
    const rootValue = isPlainObject(value) ? value : { value };
    return JSON.stringify(rootValue, null, 2);
  }

  const currentState = parseJsonObjectLoose(jsonText);
  const nextPathState = composeValueByPath(normalizedPath, value);

  if (!isPlainObject(nextPathState)) {
    return JSON.stringify(currentState, null, 2);
  }

  return JSON.stringify(mergeRecords(currentState, nextPathState), null, 2);
}

export function removeJsonObjectFieldValue(jsonText: string, path: string) {
  const normalizedPath = path.trim();

  if (!normalizedPath) {
    return "{}";
  }

  const sourceState = parseJsonObjectLoose(jsonText);
  const nextState = JSON.parse(JSON.stringify(sourceState)) as Record<string, unknown>;
  const segments = toPathSegments(normalizedPath);

  if (segments.length === 0) {
    return JSON.stringify(nextState, null, 2);
  }

  const removeAt = (node: unknown, depth: number): boolean => {
    const segment = segments[depth];
    const isLeaf = depth === segments.length - 1;

    if (Array.isArray(node)) {
      if (!isArrayIndexSegment(segment)) {
        return node.length === 0;
      }

      const index = Number(segment);
      if (!Number.isInteger(index) || index < 0 || index >= node.length) {
        return node.length === 0;
      }

      if (isLeaf) {
        node.splice(index, 1);
      } else {
        const child = node[index];
        const shouldDeleteChild = removeAt(child, depth + 1);
        if (shouldDeleteChild) {
          node.splice(index, 1);
        }
      }

      return node.length === 0;
    }

    if (!isPlainObject(node)) {
      return false;
    }

    if (!(segment in node)) {
      return Object.keys(node).length === 0;
    }

    if (isLeaf) {
      delete node[segment];
    } else {
      const child = node[segment];
      const shouldDeleteChild = removeAt(child, depth + 1);
      if (shouldDeleteChild) {
        delete node[segment];
      }
    }

    return Object.keys(node).length === 0;
  };

  removeAt(nextState, 0);
  return JSON.stringify(nextState, null, 2);
}

export function removeJsonObjectFields(jsonText: string, paths: string[]) {
  const normalizedPaths = Array.from(
    new Set(paths.map((path) => path.trim()).filter((path) => path !== ""))
  );

  return normalizedPaths.reduce(
    (currentJsonText, path) => removeJsonObjectFieldValue(currentJsonText, path),
    jsonText
  );
}

export function parseRequiredJsonObject(
  jsonText: string,
  errorKeys: RequiredJsonObjectErrorKeys
): { value: Record<string, unknown> | null; errorKey: string | null } {
  const trimmed = jsonText.trim();

  if (!trimmed) {
    return {
      value: null,
      errorKey: errorKeys.required,
    };
  }

  try {
    const parsed = JSON.parse(trimmed) as unknown;

    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      return {
        value: null,
        errorKey: errorKeys.mustBeObject,
      };
    }

    const objectValue = parsed as Record<string, unknown>;

    if (Object.keys(objectValue).length === 0) {
      return {
        value: null,
        errorKey: errorKeys.mustBeObject,
      };
    }

    return { value: objectValue, errorKey: null };
  } catch {
    return {
      value: null,
      errorKey: errorKeys.invalidJson,
    };
  }
}

export function sanitizeJsonObjectByPaths(
  value: Record<string, unknown>,
  paths: string[] | undefined
) {
  const normalizedPaths = (paths ?? [])
    .map((path) => path.trim())
    .filter((path) => path !== "");

  if (normalizedPaths.length === 0) {
    return value;
  }

  const sanitizedJsonText = removeJsonObjectFields(
    JSON.stringify(value, null, 2),
    normalizedPaths
  );

  return parseJsonObjectLoose(sanitizedJsonText);
}
