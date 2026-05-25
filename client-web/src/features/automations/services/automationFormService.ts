import type { SchemaField } from "@/features/capabilities";
import type {
  AutomationConditionDetailDto,
  AutomationTimeWindowDto,
  AutomationDayOfWeek,
  AutomationConditionOperator,
  AutomationConditionRequest,
  AutomationTimeWindowRequest,
} from "../types/automationTypes";

export type AutomationConditionDraft = {
  key: string;
  roomId: string;
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  fieldPath: string;
  operator: AutomationConditionOperator;
  compareValueText: string;
  betweenMinText: string;
  betweenMaxText: string;
};

export type AutomationTimeWindowDraft = {
  enabled: boolean;
  startTimeText: string;
  endTimeText: string;
  daysOfWeek: AutomationDayOfWeek[];
};

type BuildResult<T> =
  | { value: T; errorKey?: never }
  | { value?: never; errorKey: string };

export const DEFAULT_COOLDOWN_MS = 30000;
export const AUTOMATION_DAYS_OF_WEEK: AutomationDayOfWeek[] = [
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
  "Sunday",
];

const NUMERIC_OPERATORS: AutomationConditionOperator[] = [
  "Equals",
  "NotEquals",
  "GreaterThan",
  "GreaterThanOrEqual",
  "LessThan",
  "LessThanOrEqual",
  "Between",
];

export const BASIC_OPERATORS: AutomationConditionOperator[] = [
  "Equals",
  "NotEquals",
];

function buildDraftKey() {
  return `${Date.now()}-${Math.random().toString(16).slice(2, 10)}`;
}

export function createEmptyAutomationConditionDraft(): AutomationConditionDraft {
  return {
    key: buildDraftKey(),
    roomId: "",
    deviceId: "",
    endpointId: "",
    capabilityId: "",
    fieldPath: "",
    operator: "Equals",
    compareValueText: "",
    betweenMinText: "",
    betweenMaxText: "",
  };
}

export function createDefaultAutomationTimeWindowDraft(): AutomationTimeWindowDraft {
  return {
    enabled: false,
    startTimeText: "00:00",
    endTimeText: "23:59",
    daysOfWeek: [...AUTOMATION_DAYS_OF_WEEK],
  };
}

export function getSupportedOperators(field: SchemaField | undefined) {
  if (!field) {
    return BASIC_OPERATORS;
  }

  return field.type === "number" || field.type === "integer"
    ? NUMERIC_OPERATORS
    : BASIC_OPERATORS;
}

export function stringifyConditionValue(value: unknown) {
  if (typeof value === "boolean") {
    return value ? "true" : "false";
  }

  if (typeof value === "number" || typeof value === "string") {
    return String(value);
  }

  return "";
}

export function conditionDetailToDraft(
  condition: AutomationConditionDetailDto,
  roomId: string
): AutomationConditionDraft {
  const isBetween =
    condition.operator === "Between" &&
    condition.compareValue !== null &&
    typeof condition.compareValue === "object" &&
    !Array.isArray(condition.compareValue);
  const betweenValue = isBetween
    ? (condition.compareValue as Record<string, unknown>)
    : {};

  return {
    key: buildDraftKey(),
    roomId,
    deviceId: condition.deviceId ?? "",
    endpointId: condition.endpointId ?? "",
    capabilityId: condition.capabilityId ?? "",
    fieldPath: condition.fieldPath ?? "",
    operator: condition.operator ?? "Equals",
    compareValueText: isBetween
      ? ""
      : stringifyConditionValue(condition.compareValue),
    betweenMinText: stringifyConditionValue(betweenValue.min),
    betweenMaxText: stringifyConditionValue(betweenValue.max),
  };
}

export function timeWindowDetailToDraft(
  timeWindow: AutomationTimeWindowDto | null | undefined
): AutomationTimeWindowDraft {
  if (!timeWindow?.enabled) {
    return createDefaultAutomationTimeWindowDraft();
  }

  return {
    enabled: true,
    startTimeText: timeWindow.startTime ?? "00:00",
    endTimeText: timeWindow.endTime ?? "23:59",
    daysOfWeek: timeWindow.daysOfWeek.length > 0
      ? [...timeWindow.daysOfWeek]
      : [...AUTOMATION_DAYS_OF_WEEK],
  };
}

export function buildAutomationConditionRequest(
  draft: AutomationConditionDraft,
  field: SchemaField | undefined
): BuildResult<AutomationConditionRequest> {
  if (!draft.deviceId.trim()) {
    return { errorKey: "automations.errors.deviceIdRequired" };
  }

  if (!draft.endpointId.trim()) {
    return { errorKey: "automations.errors.endpointIdRequired" };
  }

  if (!draft.capabilityId.trim()) {
    return { errorKey: "automations.errors.capabilityIdRequired" };
  }

  if (!draft.fieldPath.trim()) {
    return { errorKey: "automations.errors.fieldRequired" };
  }

  const supportedOperators = getSupportedOperators(field);
  if (!supportedOperators.includes(draft.operator)) {
    return { errorKey: "automations.errors.operatorInvalid" };
  }

  if (draft.operator === "Between") {
    const min = Number(draft.betweenMinText.trim());
    const max = Number(draft.betweenMaxText.trim());

    if (!Number.isFinite(min) || !Number.isFinite(max) || min > max) {
      return { errorKey: "automations.errors.compareValueInvalid" };
    }

    return {
      value: {
        deviceId: draft.deviceId,
        endpointId: draft.endpointId,
        capabilityId: draft.capabilityId,
        fieldPath: draft.fieldPath,
        operator: draft.operator,
        compareValue: { min, max },
      },
    };
  }

  const rawValue = draft.compareValueText.trim();
  let compareValue: unknown = rawValue;

  if (field?.type === "boolean") {
    compareValue = rawValue === "true";
  } else if (field?.type === "number" || field?.type === "integer") {
    const parsed = Number(rawValue);
    if (!Number.isFinite(parsed)) {
      return { errorKey: "automations.errors.compareValueInvalid" };
    }

    compareValue = field.type === "integer" ? Math.trunc(parsed) : parsed;
  } else if (rawValue === "") {
    return { errorKey: "automations.errors.compareValueInvalid" };
  }

  return {
    value: {
      deviceId: draft.deviceId,
      endpointId: draft.endpointId,
      capabilityId: draft.capabilityId,
      fieldPath: draft.fieldPath,
      operator: draft.operator,
      compareValue,
    },
  };
}

export function buildAutomationTimeWindowRequest(
  draft: AutomationTimeWindowDraft
): BuildResult<AutomationTimeWindowRequest> {
  if (!draft.enabled) {
    return {
      value: {
        enabled: false,
        startTime: null,
        endTime: null,
        daysOfWeek: null,
      },
    };
  }

  if (!isValidTimeText(draft.startTimeText) || !isValidTimeText(draft.endTimeText)) {
    return { errorKey: "automations.errors.timeInvalid" };
  }

  if (draft.daysOfWeek.length === 0) {
    return { errorKey: "automations.errors.daysRequired" };
  }

  return {
    value: {
      enabled: true,
      startTime: draft.startTimeText,
      endTime: draft.endTimeText,
      daysOfWeek: draft.daysOfWeek,
    },
  };
}

function isValidTimeText(value: string) {
  const match = /^([01]\d|2[0-3]):([0-5]\d)$/.exec(value.trim());
  return Boolean(match);
}
