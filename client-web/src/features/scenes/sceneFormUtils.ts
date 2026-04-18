import {
  composeValueByPath,
  getValueByPath,
} from "@/features/capabilities";
import type {
  SceneDetailSideEffectDto,
  SceneDetailTargetDto,
  SceneSideEffectRequest,
  SceneSideEffectTiming,
  SceneTargetRequest,
} from "./scenes.types";

export type SceneTargetDraft = {
  key: string;
  roomId: string;
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  desiredStateText: string;
};

export type SceneSideEffectDraft = {
  key: string;
  roomId: string;
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  operation: string;
  timing: SceneSideEffectTiming;
  delayMsText: string;
  paramsText: string;
};

const DEFAULT_SIDE_EFFECT_TIMING: SceneSideEffectTiming = "AfterVerify";

function buildDraftKey() {
  return `${Date.now()}-${Math.random().toString(16).slice(2, 10)}`;
}

export function createEmptySceneTargetDraft(): SceneTargetDraft {
  return {
    key: buildDraftKey(),
    roomId: "",
    deviceId: "",
    endpointId: "",
    capabilityId: "",
    desiredStateText: "{}",
  };
}

export function createEmptySceneSideEffectDraft(): SceneSideEffectDraft {
  return {
    key: buildDraftKey(),
    roomId: "",
    deviceId: "",
    endpointId: "",
    capabilityId: "",
    operation: "",
    timing: DEFAULT_SIDE_EFFECT_TIMING,
    delayMsText: "0",
    paramsText: "{}",
  };
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

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

function mergeRecords(
  target: Record<string, unknown>,
  source: Record<string, unknown>
): Record<string, unknown> {
  const merged: Record<string, unknown> = { ...target };

  Object.entries(source).forEach(([key, value]) => {
    const existing = merged[key];

    if (isPlainObject(existing) && isPlainObject(value)) {
      merged[key] = mergeRecords(existing, value);
      return;
    }

    merged[key] = value;
  });

  return merged;
}

export function parseDesiredStateObjectLoose(
  desiredStateText: string
): Record<string, unknown> {
  const trimmed = desiredStateText.trim();

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

export function parseJsonObjectLoose(jsonText: string): Record<string, unknown> {
  return parseDesiredStateObjectLoose(jsonText);
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
    const rootValue = isPlainObject(value)
      ? value
      : { value };
    return JSON.stringify(rootValue, null, 2);
  }

  const currentState = parseJsonObjectLoose(jsonText);
  const nextPathState = composeValueByPath(normalizedPath, value);

  if (!isPlainObject(nextPathState)) {
    return JSON.stringify(currentState, null, 2);
  }

  return JSON.stringify(mergeRecords(currentState, nextPathState), null, 2);
}

export function removeJsonObjectFieldValue(
  jsonText: string,
  path: string
) {
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

export function removeJsonObjectFields(
  jsonText: string,
  paths: string[]
) {
  const normalizedPaths = Array.from(
    new Set(
      paths
        .map((path) => path.trim())
        .filter((path) => path !== "")
    )
  );

  return normalizedPaths.reduce(
    (currentJsonText, path) => removeJsonObjectFieldValue(currentJsonText, path),
    jsonText
  );
}

export function getDesiredStateFieldValue(
  desiredStateText: string,
  path: string
): unknown {
  return getJsonObjectFieldValue(desiredStateText, path);
}

export function updateDesiredStateFieldValue(
  desiredStateText: string,
  path: string,
  value: unknown
) {
  return updateJsonObjectFieldValue(desiredStateText, path, value);
}

export function removeDesiredStateFieldValue(
  desiredStateText: string,
  path: string
) {
  return removeJsonObjectFieldValue(desiredStateText, path);
}

export function getSideEffectParamFieldValue(
  paramsText: string,
  path: string
): unknown {
  return getJsonObjectFieldValue(paramsText, path);
}

export function updateSideEffectParamFieldValue(
  paramsText: string,
  path: string,
  value: unknown
) {
  return updateJsonObjectFieldValue(paramsText, path, value);
}

export function removeSideEffectParamFieldValue(
  paramsText: string,
  path: string
) {
  return removeJsonObjectFieldValue(paramsText, path);
}

export function sceneDetailTargetToDraft(
  action: SceneDetailTargetDto
): SceneTargetDraft {
  return {
    key: action.id,
    roomId: "",
    deviceId: action.deviceId,
    endpointId: action.endpointId,
    capabilityId: action.capabilityId,
    desiredStateText: JSON.stringify(action.desiredState, null, 2),
  };
}

export function sceneDetailSideEffectToDraft(
  sideEffect: SceneDetailSideEffectDto
): SceneSideEffectDraft {
  return {
    key: sideEffect.id,
    roomId: "",
    deviceId: sideEffect.deviceId,
    endpointId: sideEffect.endpointId,
    capabilityId: sideEffect.capabilityId,
    operation: sideEffect.operation,
    timing: sideEffect.timing,
    delayMsText: String(sideEffect.delayMs),
    paramsText: JSON.stringify(sideEffect.params, null, 2),
  };
}

export function parseDesiredStateObject(
  desiredStateText: string
): { value: Record<string, unknown> | null; errorKey: string | null; } {
  const trimmed = desiredStateText.trim();

  if (!trimmed) {
    return {
      value: null,
      errorKey: "scenes.errors.desiredStateRequired",
    };
  }

  try {
    const parsed = JSON.parse(trimmed) as unknown;

    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      return {
        value: null,
        errorKey: "scenes.errors.desiredStateMustBeObject",
      };
    }

    const objectValue = parsed as Record<string, unknown>;

    if (Object.keys(objectValue).length === 0) {
      return {
        value: null,
        errorKey: "scenes.errors.desiredStateMustBeObject",
      };
    }

    return { value: objectValue, errorKey: null };
  } catch {
    return {
      value: null,
      errorKey: "scenes.errors.desiredStateInvalidJson",
    };
  }
}

type BuildSceneRequestOptions = {
  readOnlyPaths?: string[];
};

function sanitizeJsonObjectByPaths(
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

export function buildSceneTargetRequest(
  action: SceneTargetDraft,
  options?: BuildSceneRequestOptions
): { value: SceneTargetRequest | null; errorKey: string | null; } {
  if (!action.deviceId.trim()) {
    return {
      value: null,
      errorKey: "scenes.errors.deviceIdRequired",
    };
  }

  if (!action.capabilityId.trim()) {
    return {
      value: null,
      errorKey: "scenes.errors.capabilityIdRequired",
    };
  }

  if (!action.endpointId.trim()) {
    return {
      value: null,
      errorKey: "scenes.errors.endpointIdRequired",
    };
  }

  const desiredState = parseDesiredStateObject(action.desiredStateText);

  if (!desiredState.value || desiredState.errorKey) {
    return {
      value: null,
      errorKey: desiredState.errorKey,
    };
  }

  const sanitizedDesiredState = sanitizeJsonObjectByPaths(
    desiredState.value,
    options?.readOnlyPaths
  );

  if (Object.keys(sanitizedDesiredState).length === 0) {
    return {
      value: null,
      errorKey: "scenes.errors.desiredStateMustBeObject",
    };
  }

  return {
    value: {
      deviceId: action.deviceId.trim(),
      endpointId: action.endpointId.trim(),
      capabilityId: action.capabilityId.trim(),
      desiredState: sanitizedDesiredState,
    },
    errorKey: null,
  };
}

export function buildSceneSideEffectRequest(
  sideEffect: SceneSideEffectDraft,
  options?: BuildSceneRequestOptions
): { value: SceneSideEffectRequest | null; errorKey: string | null; } {
  if (!sideEffect.deviceId.trim()) {
    return {
      value: null,
      errorKey: "scenes.errors.deviceIdRequired",
    };
  }

  if (!sideEffect.endpointId.trim()) {
    return {
      value: null,
      errorKey: "scenes.errors.endpointIdRequired",
    };
  }

  if (!sideEffect.capabilityId.trim()) {
    return {
      value: null,
      errorKey: "scenes.errors.capabilityIdRequired",
    };
  }

  if (!sideEffect.operation.trim()) {
    return {
      value: null,
      errorKey: "scenes.errors.operationRequired",
    };
  }

  const parsedDelayMs = Number(sideEffect.delayMsText.trim());
  if (!Number.isFinite(parsedDelayMs) || parsedDelayMs < 0) {
    return {
      value: null,
      errorKey: "scenes.errors.delayMsInvalid",
    };
  }

  let parsedParams: unknown;
  try {
    parsedParams = JSON.parse(sideEffect.paramsText.trim() || "{}");
  } catch {
    return {
      value: null,
      errorKey: "scenes.errors.paramsInvalidJson",
    };
  }

  if (!parsedParams || typeof parsedParams !== "object" || Array.isArray(parsedParams)) {
    return {
      value: null,
      errorKey: "scenes.errors.paramsMustBeObject",
    };
  }

  const sanitizedParams = sanitizeJsonObjectByPaths(
    parsedParams as Record<string, unknown>,
    options?.readOnlyPaths
  );

  return {
    value: {
      deviceId: sideEffect.deviceId.trim(),
      endpointId: sideEffect.endpointId.trim(),
      capabilityId: sideEffect.capabilityId.trim(),
      operation: sideEffect.operation.trim(),
      timing: sideEffect.timing,
      delayMs: Math.trunc(parsedDelayMs),
      params: sanitizedParams,
    },
    errorKey: null,
  };
}
