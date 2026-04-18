import Ajv, { type ErrorObject, type ValidateFunction } from "ajv";
import type { JsonSchemaObject } from "@/features/capabilities/capabilities.types";

export type OperationRule = {
  schema: JsonSchemaObject;
  type: string | null;
  enumValues: unknown[];
  min: number | null;
  max: number | null;
  step: number | null;
  minLength: number | null;
  maxLength: number | null;
  valueRequired: boolean;
  allowsNull: boolean;
};

export type CommandValidationResult = {
  value: unknown | null;
  errorKey: string | null;
  errorOptions?: Record<string, unknown>;
};

const ajv = new Ajv({ allErrors: true, strict: false });
const schemaValidatorCache = new WeakMap<JsonSchemaObject, ValidateFunction>();

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function toSchemaPathSegments(path: string): string[] {
  return path
    .replace(/\[(\d+)\]/g, ".$1")
    .split(".")
    .map((segment) => segment.trim())
    .filter((segment) => segment !== "");
}

function resolveOperationKey(
  operations: Record<string, JsonSchemaObject> | null,
  operation: string
) {
  if (!operations || !operation.trim()) {
    return null;
  }

  const exactSchema = operations[operation];
  if (exactSchema) {
    return operation;
  }

  return (
    Object.keys(operations).find(
      (key) => key.trim().toLowerCase() === operation.trim().toLowerCase()
    ) ?? null
  );
}

function getSchemaValidator(schema: JsonSchemaObject): ValidateFunction | null {
  const cached = schemaValidatorCache.get(schema);
  if (cached) {
    return cached;
  }

  try {
    const validator = ajv.compile(schema);
    schemaValidatorCache.set(schema, validator);
    return validator;
  } catch {
    return null;
  }
}

function toSchemaTypes(schema: JsonSchemaObject): string[] {
  const schemaRecord = schema as Record<string, unknown>;
  const rawType = schemaRecord.type;

  if (typeof rawType === "string") {
    return [rawType.toLowerCase()];
  }

  if (Array.isArray(rawType)) {
    return rawType
      .filter((item): item is string => typeof item === "string")
      .map((item) => item.toLowerCase());
  }

  const variants = [schemaRecord.oneOf, schemaRecord.anyOf]
    .filter((candidate) => Array.isArray(candidate))
    .flatMap((candidate) => candidate as unknown[])
    .filter((item): item is Record<string, unknown> =>
      typeof item === "object" && item !== null && !Array.isArray(item)
    )
    .map((item) => item.type)
    .flatMap((typeCandidate) => {
      if (typeof typeCandidate === "string") {
        return [typeCandidate.toLowerCase()];
      }

      if (Array.isArray(typeCandidate)) {
        return typeCandidate
          .filter((item): item is string => typeof item === "string")
          .map((item) => item.toLowerCase());
      }

      return [];
    });

  return variants;
}

function getPrimarySchemaType(types: string[]) {
  return (
    types.find((type) => type !== "null") ??
    types[0] ??
    null
  );
}

function isSchemaValueRequired(schema: JsonSchemaObject, types: string[]) {
  const schemaRecord = schema as Record<string, unknown>;

  if (Object.keys(schemaRecord).length === 0) {
    return false;
  }

  if (types.length === 0) {
    return false;
  }

  return !types.includes("null");
}

function toNumberOrNull(value: unknown) {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function getSchemaVariantRecords(schemaRecord: Record<string, unknown>) {
  return [schemaRecord.oneOf, schemaRecord.anyOf]
    .filter((candidate): candidate is unknown[] => Array.isArray(candidate))
    .flatMap((candidate) => candidate)
    .filter((candidate): candidate is Record<string, unknown> =>
      typeof candidate === "object" && candidate !== null && !Array.isArray(candidate)
    );
}

function getNumericConstraint(
  schemaRecord: Record<string, unknown>,
  key: "minimum" | "maximum" | "multipleOf" | "step",
  requirePositive = false
) {
  const candidates = [schemaRecord, ...getSchemaVariantRecords(schemaRecord)];

  for (const candidate of candidates) {
    const value = candidate[key];

    if (
      typeof value === "number" &&
      Number.isFinite(value) &&
      (!requirePositive || value > 0)
    ) {
      return value;
    }
  }

  return null;
}

function getNumericStepConstraint(schemaRecord: Record<string, unknown>) {
  const multipleOf = getNumericConstraint(schemaRecord, "multipleOf", true);
  if (typeof multipleOf === "number") {
    return multipleOf;
  }

  return getNumericConstraint(schemaRecord, "step", true);
}

function toOperationRule(schema: JsonSchemaObject): OperationRule {
  const schemaRecord = schema as Record<string, unknown>;
  const schemaTypes = toSchemaTypes(schema);

  return {
    schema,
    type: getPrimarySchemaType(schemaTypes),
    enumValues: Array.isArray(schemaRecord.enum) ? schemaRecord.enum : [],
    min: getNumericConstraint(schemaRecord, "minimum"),
    max: getNumericConstraint(schemaRecord, "maximum"),
    step: getNumericStepConstraint(schemaRecord),
    minLength: toNumberOrNull(schemaRecord.minLength),
    maxLength: toNumberOrNull(schemaRecord.maxLength),
    valueRequired: isSchemaValueRequired(schema, schemaTypes),
    allowsNull: schemaTypes.includes("null"),
  };
}

function getFirstAjvError(errors: ErrorObject[] | null | undefined) {
  return Array.isArray(errors) && errors.length > 0 ? errors[0] : null;
}

function mapAjvError(error: ErrorObject | null): Omit<CommandValidationResult, "value"> {
  if (!error) {
    return { errorKey: "errors.invalidCommandValue" };
  }

  if (error.keyword === "enum") {
    return { errorKey: "errors.valueNotInAllowedEnum" };
  }

  if (error.keyword === "minimum" || error.keyword === "exclusiveMinimum") {
    const minimum = (error.params as { limit?: unknown; }).limit;
    return {
      errorKey: "errors.valueMin",
      errorOptions: { min: minimum },
    };
  }

  if (error.keyword === "maximum" || error.keyword === "exclusiveMaximum") {
    const maximum = (error.params as { limit?: unknown; }).limit;
    return {
      errorKey: "errors.valueMax",
      errorOptions: { max: maximum },
    };
  }

  if (error.keyword === "minLength") {
    const minLength = (error.params as { limit?: unknown; }).limit;
    return {
      errorKey: "errors.valueMinLength",
      errorOptions: { minLength },
    };
  }

  if (error.keyword === "maxLength") {
    const maxLength = (error.params as { limit?: unknown; }).limit;
    return {
      errorKey: "errors.valueMaxLength",
      errorOptions: { maxLength },
    };
  }

  return { errorKey: "errors.invalidCommandValue" };
}

function isMissingRawValue(rawValue: string) {
  return rawValue.trim() === "";
}

function isExplicitNullLiteral(rawValue: string) {
  return rawValue.trim().toLowerCase() === "null";
}

export function getOperationRule(
  operations: Record<string, JsonSchemaObject> | null,
  operation: string
): OperationRule | null {
  const schema = getOperationSchema(operations, operation);
  return schema ? toOperationRule(schema) : null;
}

export function getOperationSchema(
  operations: Record<string, JsonSchemaObject> | null,
  operation: string
): JsonSchemaObject | null {
  const schemaKey = resolveOperationKey(operations, operation);

  if (!operations || !schemaKey) {
    return null;
  }

  return operations[schemaKey] ?? null;
}

export function getSchemaAtPath(
  schema: JsonSchemaObject | null,
  path: string | null | undefined
): JsonSchemaObject | null {
  if (!schema) {
    return null;
  }

  const normalizedPath = path?.trim();
  if (!normalizedPath) {
    return schema;
  }

  const segments = toSchemaPathSegments(normalizedPath);
  if (segments.length === 0) {
    return schema;
  }

  let currentSchema: unknown = schema;

  for (const segment of segments) {
    if (!isPlainObject(currentSchema)) {
      return null;
    }

    const properties = currentSchema.properties;
    if (!isPlainObject(properties)) {
      return null;
    }

    currentSchema = properties[segment];
  }

  return isPlainObject(currentSchema) ? (currentSchema as JsonSchemaObject) : null;
}

export function getOperationRuleForPath(
  operations: Record<string, JsonSchemaObject> | null,
  operation: string,
  path: string | null | undefined
): OperationRule | null {
  const operationSchema = getOperationSchema(operations, operation);
  const schemaAtPath = getSchemaAtPath(operationSchema, path);

  return schemaAtPath ? toOperationRule(schemaAtPath) : null;
}

export function resolveSupportedOperation(
  supportedOperations: string[],
  requestedOperation: string | null | undefined
) {
  const normalizedRequestedOperation = requestedOperation?.trim().toLowerCase();

  if (!normalizedRequestedOperation) {
    return null;
  }

  return (
    supportedOperations.find(
      (operation) => operation.trim().toLowerCase() === normalizedRequestedOperation
    ) ?? null
  );
}

export function normalizeRuleType(rule: OperationRule | null) {
  return rule?.type?.trim().toLowerCase() ?? null;
}

export function defaultValueForRule(rule: OperationRule | null) {
  if (!rule) {
    return "";
  }

  if (Array.isArray(rule.enumValues) && rule.enumValues.length > 0) {
    return String(rule.enumValues[0]);
  }

  const type = normalizeRuleType(rule);

  if (type === "boolean") {
    return "true";
  }

  if (type === "number" || type === "integer") {
    if (typeof rule.min === "number") {
      return String(rule.min);
    }

    return "0";
  }

  if (type === "array") {
    return "[]";
  }

  if (type === "object") {
    return "{\n  \n}";
  }

  return "";
}

export function getNumericStepForRule(rule: OperationRule | null) {
  const step = rule?.step;
  return typeof step === "number" && step > 0 ? step : 1;
}

export function hasNumericRange(rule: OperationRule | null) {
  return typeof rule?.min === "number" && typeof rule?.max === "number" && rule.min <= rule.max;
}

export function getDefaultNumericValue(rule: OperationRule | null) {
  if (!rule) {
    return "0";
  }

  if (typeof rule.min === "number") {
    return String(rule.min);
  }

  return "0";
}

export function buildCommandValue(rule: OperationRule | null, rawValue: string): unknown | null {
  if (isMissingRawValue(rawValue)) {
    return null;
  }

  const type = normalizeRuleType(rule);

  if (type === "boolean") {
    const normalized = rawValue.trim().toLowerCase();
    if (normalized === "true") return true;
    if (normalized === "false") return false;
    return null;
  }

  if (type === "number") {
    const parsed = Number(rawValue);
    return Number.isFinite(parsed) ? parsed : null;
  }

  if (type === "integer") {
    const parsed = Number(rawValue);
    return Number.isInteger(parsed) ? parsed : null;
  }

  if (type === "array" || type === "object") {
    try {
      return JSON.parse(rawValue) as unknown;
    } catch {
      return null;
    }
  }

  if (type === "null") {
    return isExplicitNullLiteral(rawValue) ? null : null;
  }

  return rawValue;
}

export function validateCommandValue(
  rule: OperationRule | null,
  rawValue: string
): CommandValidationResult {
  if (!rule) {
    return {
      value: null,
      errorKey: "errors.operationUnavailableInSchema",
    };
  }

  if (isMissingRawValue(rawValue)) {
    if (rule.valueRequired) {
      return {
        value: null,
        errorKey: "errors.valueRequiredForOperation",
      };
    }

    return {
      value: null,
      errorKey: null,
    };
  }

  const parsedValue = buildCommandValue(rule, rawValue);
  const type = normalizeRuleType(rule);

  if (parsedValue === null && !(isExplicitNullLiteral(rawValue) && rule.allowsNull)) {
    if (type === "boolean") {
      return { value: null, errorKey: "errors.valueMustBeTrueOrFalse" };
    }

    if (type === "number") {
      return { value: null, errorKey: "errors.valueMustBeNumber" };
    }

    if (type === "integer") {
      return { value: null, errorKey: "errors.valueMustBeInteger" };
    }

    if (type === "array" || type === "object") {
      return { value: null, errorKey: "errors.valueMustBeValidJson" };
    }
  }

  const valueToValidate = isExplicitNullLiteral(rawValue) && rule.allowsNull
    ? null
    : parsedValue;

  const validator = getSchemaValidator(rule.schema);
  if (!validator) {
    return {
      value: valueToValidate,
      errorKey: null,
    };
  }

  const valid = validator(valueToValidate);
  if (!valid) {
    const mappedError = mapAjvError(getFirstAjvError(validator.errors));
    return {
      value: valueToValidate,
      ...mappedError,
    };
  }

  return {
    value: valueToValidate,
    errorKey: null,
  };
}

export function isStructuredCapabilityState(state: unknown) {
  return typeof state === "object" && state !== null;
}

export function formatCompactValue(value: unknown) {
  if (value === null || value === undefined) return "null";
  if (typeof value === "string") return value;
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  if (Array.isArray(value)) return `[${value.length} items]`;
  if (typeof value === "object") return `{${Object.keys(value as Record<string, unknown>).length} fields}`;
  return String(value);
}

export function formatCapabilityState(state: unknown) {
  if (state === null || state === undefined) {
    return "Unavailable";
  }

  if (typeof state === "string") {
    return state;
  }

  if (typeof state === "number" || typeof state === "boolean") {
    return String(state);
  }

  return JSON.stringify(state, null, 2);
}

export function getBooleanStateValue(state: unknown) {
  if (typeof state === "boolean") {
    return state;
  }

  if (state && typeof state === "object" && !Array.isArray(state)) {
    const record = state as Record<string, unknown>;
    const commonKeys = ["value", "on", "isOn", "power", "enabled"];
    for (const key of commonKeys) {
      if (typeof record[key] === "boolean") {
        return record[key] as boolean;
      }
    }
  }

  return null;
}

export function getNumericStateValue(state: unknown) {
  if (typeof state === "number") {
    return String(state);
  }

  if (state && typeof state === "object" && !Array.isArray(state)) {
    const record = state as Record<string, unknown>;
    if (typeof record.value === "number") {
      return String(record.value);
    }
  }

  return null;
}

export function isIntegerSliderRule(rule: OperationRule | null) {
  return isNumericSliderRule(rule);
}

export function isNumericSliderRule(rule: OperationRule | null) {
  const ruleType = normalizeRuleType(rule);
  return (ruleType === "integer" || ruleType === "number") && hasNumericRange(rule);
}

export function getSetOperation(operations: string[]) {
  return operations.find((operation) => operation.trim().toLowerCase() === "set") ?? null;
}
