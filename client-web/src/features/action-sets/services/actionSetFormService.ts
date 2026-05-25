import {
  getJsonObjectFieldValue,
  parseRequiredJsonObject,
  removeJsonObjectFieldValue,
  sanitizeJsonObjectByPaths,
  updateJsonObjectFieldValue,
} from "@/features/capability-builder";
import type {
  ActionDto,
  ActionExecutionMode,
  ActionRequest,
  ActionSetDto,
  ActionSetRequest,
  InvokeOperationActionRequest,
  SetStateActionRequest,
} from "../types/actionSetTypes";

type ActionDraftBase = {
  key: string;
  roomId: string;
  deviceId: string;
  endpointId: string;
  capabilityId: string;
};

export type SetStateActionDraft = ActionDraftBase & {
  type: "setState";
  stateText: string;
  optionsText: string;
};

export type InvokeOperationActionDraft = ActionDraftBase & {
  type: "invokeOperation";
  operation: string;
  payloadText: string;
};

export type ActionDraft = SetStateActionDraft | InvokeOperationActionDraft;

export type ActionSetDraft = {
  executionPolicy: {
    mode: ActionExecutionMode;
    continueOnError: boolean;
  };
  actions: ActionDraft[];
  hooks: {
    before: ActionDraft[];
    onSuccess: ActionDraft[];
    onFailure: ActionDraft[];
  };
};

type BuildResult<T> =
  | { value: T; errorKey?: never; }
  | { value?: never; errorKey: string; };

function buildDraftKey() {
  return `${Date.now()}-${Math.random().toString(16).slice(2, 10)}`;
}

export function createEmptySetStateActionDraft(): SetStateActionDraft {
  return {
    key: buildDraftKey(),
    type: "setState",
    roomId: "",
    deviceId: "",
    endpointId: "",
    capabilityId: "",
    stateText: "{}",
    optionsText: "{}",
  };
}

export function createEmptyInvokeOperationActionDraft(): InvokeOperationActionDraft {
  return {
    key: buildDraftKey(),
    type: "invokeOperation",
    roomId: "",
    deviceId: "",
    endpointId: "",
    capabilityId: "",
    operation: "",
    payloadText: "{}",
  };
}

export function createEmptyActionSetDraft(): ActionSetDraft {
  return {
    executionPolicy: {
      mode: "sequential",
      continueOnError: false,
    },
    actions: [],
    hooks: {
      before: [],
      onSuccess: [],
      onFailure: [],
    },
  };
}

export function getActionStateFieldValue(stateText: string, path: string): unknown {
  return getJsonObjectFieldValue(stateText, path);
}

export function updateActionStateFieldValue(
  stateText: string,
  path: string,
  value: unknown
) {
  return updateJsonObjectFieldValue(stateText, path, value);
}

export function removeActionStateFieldValue(stateText: string, path: string) {
  return removeJsonObjectFieldValue(stateText, path);
}

export function getActionPayloadFieldValue(payloadText: string, path: string): unknown {
  return getJsonObjectFieldValue(payloadText, path);
}

export function updateActionPayloadFieldValue(
  payloadText: string,
  path: string,
  value: unknown
) {
  return updateJsonObjectFieldValue(payloadText, path, value);
}

export function removeActionPayloadFieldValue(payloadText: string, path: string) {
  return removeJsonObjectFieldValue(payloadText, path);
}

function actionDtoToDraft(action: ActionDto): ActionDraft {
  const base = {
    key: action.id,
    roomId: "",
    deviceId: action.target.deviceId,
    endpointId: action.target.endpointId,
    capabilityId: action.target.capabilityId,
  };

  if (action.type === "setState") {
    return {
      ...base,
      type: "setState",
      stateText: JSON.stringify(action.state ?? {}, null, 2),
      optionsText: JSON.stringify(action.options ?? {}, null, 2),
    };
  }

  return {
    ...base,
    type: "invokeOperation",
    operation: action.operation,
    payloadText: JSON.stringify(action.payload ?? {}, null, 2),
  };
}

export function actionSetDtoToDraft(
  actionSet: ActionSetDto | null | undefined
): ActionSetDraft {
  return {
    executionPolicy: {
      mode: actionSet?.executionPolicy?.mode ?? "sequential",
      continueOnError: actionSet?.executionPolicy?.continueOnError ?? false,
    },
    actions: (actionSet?.actions ?? []).map(actionDtoToDraft),
    hooks: {
      before: (actionSet?.hooks?.before ?? []).map(actionDtoToDraft),
      onSuccess: (actionSet?.hooks?.onSuccess ?? []).map(actionDtoToDraft),
      onFailure: (actionSet?.hooks?.onFailure ?? []).map(actionDtoToDraft),
    },
  };
}

function parseJsonObject(
  text: string,
  errorKeys: {
    required: string;
    invalidJson: string;
    mustBeObject: string;
  }
): BuildResult<Record<string, unknown>> {
  const result = parseRequiredJsonObject(text, errorKeys);

  return result.value && !result.errorKey
    ? { value: result.value }
    : { errorKey: result.errorKey ?? errorKeys.invalidJson };
}

function parseJsonObjectAllowEmpty(text: string): BuildResult<Record<string, unknown>> {
  const trimmed = text.trim();

  if (!trimmed) {
    return { value: {} };
  }

  try {
    const parsed = JSON.parse(trimmed) as unknown;

    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      return { errorKey: "scenes.errors.actionOptionsMustBeObject" };
    }

    return { value: parsed as Record<string, unknown> };
  } catch {
    return { errorKey: "scenes.errors.actionOptionsInvalidJson" };
  }
}

type BuildActionOptions = {
  readOnlyPaths?: string[];
};

function buildSetStateActionRequest(
  action: SetStateActionDraft,
  options?: BuildActionOptions
): BuildResult<SetStateActionRequest> {
  const target = buildActionTarget(action);
  if (!target.value) {
    return { errorKey: target.errorKey };
  }

  const state = parseJsonObject(action.stateText, {
    required: "scenes.errors.desiredStateRequired",
    invalidJson: "scenes.errors.desiredStateInvalidJson",
    mustBeObject: "scenes.errors.desiredStateMustBeObject",
  });
  if (!state.value) {
    return { errorKey: state.errorKey };
  }

  const sanitizedState = sanitizeJsonObjectByPaths(state.value, options?.readOnlyPaths);
  if (Object.keys(sanitizedState).length === 0) {
    return { errorKey: "scenes.errors.desiredStateMustBeObject" };
  }

  const parsedOptions = parseJsonObjectAllowEmpty(action.optionsText || "{}");
  if (!parsedOptions.value) {
    return { errorKey: parsedOptions.errorKey };
  }

  return {
    value: {
      type: "setState",
      target: target.value,
      state: sanitizedState,
      options: parsedOptions.value,
    },
  };
}

function buildInvokeOperationActionRequest(
  action: InvokeOperationActionDraft,
  options?: BuildActionOptions
): BuildResult<InvokeOperationActionRequest> {
  const target = buildActionTarget(action);
  if (!target.value) {
    return { errorKey: target.errorKey };
  }

  if (!action.operation.trim()) {
    return { errorKey: "scenes.errors.operationRequired" };
  }

  const payload = parseJsonObject(action.payloadText || "{}", {
    required: "scenes.errors.paramsRequired",
    invalidJson: "scenes.errors.paramsInvalidJson",
    mustBeObject: "scenes.errors.paramsMustBeObject",
  });
  if (!payload.value) {
    return { errorKey: payload.errorKey };
  }

  return {
    value: {
      type: "invokeOperation",
      target: target.value,
      operation: action.operation.trim(),
      payload: sanitizeJsonObjectByPaths(payload.value, options?.readOnlyPaths),
    },
  };
}

function buildActionTarget(
  action: Pick<ActionDraft, "deviceId" | "endpointId" | "capabilityId">
): BuildResult<ActionRequest["target"]> {
  if (!action.deviceId.trim()) {
    return { errorKey: "scenes.errors.deviceIdRequired" };
  }

  if (!action.endpointId.trim()) {
    return { errorKey: "scenes.errors.endpointIdRequired" };
  }

  if (!action.capabilityId.trim()) {
    return { errorKey: "scenes.errors.capabilityIdRequired" };
  }

  return {
    value: {
      deviceId: action.deviceId.trim(),
      endpointId: action.endpointId.trim(),
      capabilityId: action.capabilityId.trim(),
    },
  };
}

function buildActionRequest(
  action: ActionDraft,
  options?: BuildActionOptions
): BuildResult<ActionRequest> {
  return action.type === "setState"
    ? buildSetStateActionRequest(action, options)
    : buildInvokeOperationActionRequest(action, options);
}

function buildActions(
  actions: ActionDraft[],
  options?: {
    getReadOnlyPaths?: (action: ActionDraft) => string[];
  }
): BuildResult<ActionRequest[]> {
  const requests: ActionRequest[] = [];

  for (const action of actions) {
    const parsed = buildActionRequest(action, {
      readOnlyPaths: options?.getReadOnlyPaths?.(action),
    });
    if (!parsed.value) {
      return { errorKey: parsed.errorKey };
    }

    requests.push(parsed.value);
  }

  return { value: requests };
}

export function buildActionSetRequest(
  draft: ActionSetDraft,
  options?: {
    getReadOnlyPaths?: (action: ActionDraft) => string[];
  }
): BuildResult<ActionSetRequest> {
  if (draft.actions.length === 0) {
    return { errorKey: "scenes.errors.atLeastOneActionRequired" };
  }

  const mainActions = buildActions(draft.actions, options);
  if (!mainActions.value) {
    return { errorKey: mainActions.errorKey };
  }

  const beforeHooks = buildActions(draft.hooks.before, options);
  if (!beforeHooks.value) {
    return { errorKey: beforeHooks.errorKey };
  }

  const successHooks = buildActions(draft.hooks.onSuccess, options);
  if (!successHooks.value) {
    return { errorKey: successHooks.errorKey };
  }

  const failureHooks = buildActions(draft.hooks.onFailure, options);
  if (!failureHooks.value) {
    return { errorKey: failureHooks.errorKey };
  }

  return {
    value: {
      executionPolicy: {
        mode: draft.executionPolicy.mode,
        continueOnError:
          draft.executionPolicy.mode === "sequential"
            ? draft.executionPolicy.continueOnError
            : false,
      },
      actions: mainActions.value,
      hooks: {
        before: beforeHooks.value,
        onSuccess: successHooks.value,
        onFailure: failureHooks.value,
      },
    },
  };
}
