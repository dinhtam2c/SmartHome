import {
  composeValueByPath,
  getValueByPath,
  resolveCapabilityFieldEditorRender,
} from "@/features/capabilities";
import { isPlainObject, mergeRecords } from "@/shared/lib/objectUtils";
import type { CommandValidationResult, OperationRule } from "./deviceCapabilityService";
import {
  defaultValueForRule,
  getOperationRule,
  getOperationRuleForPath,
  getOperationSchema,
  normalizeRuleType,
  validateCommandValue,
} from "./deviceCapabilityService";

export type OperationField = {
  path: string;
  label: string;
  required: boolean;
  rule: OperationRule | null;
  unsupported: boolean;
};

export type OperationFieldState = OperationField & {
  rawValue: string;
  validation: CommandValidationResult;
};

type OperationFieldLike = {
  path: string;
  rule: OperationRule | null;
};

export type OperationFieldRenderInput<TField extends OperationFieldLike> = TField & {
  type: string | null;
  enumValues: unknown[];
  min: number | null;
  max: number | null;
};

function buildOperationFieldRenderInputs<TField extends OperationFieldLike>(
  fields: TField[]
): OperationFieldRenderInput<TField>[] {
  return fields.map((field) => ({
    ...field,
    type: normalizeRuleType(field.rule),
    enumValues: field.rule?.enumValues ?? [],
    min: field.rule?.min ?? null,
    max: field.rule?.max ?? null,
  }));
}

export function resolveOperationFieldRenderPlan<TField extends OperationFieldLike>(
  capabilityId: string,
  fields: TField[],
  operation?: string | null
) {
  return resolveCapabilityFieldEditorRender(
    capabilityId,
    buildOperationFieldRenderInputs(fields),
    operation
  );
}

function getSchemaPrimaryType(schema: Record<string, unknown>) {
  const schemaType = schema.type;

  if (typeof schemaType === "string") {
    return schemaType.trim().toLowerCase();
  }

  if (Array.isArray(schemaType)) {
    const normalized = schemaType
      .filter((candidate): candidate is string => typeof candidate === "string")
      .map((candidate) => candidate.trim().toLowerCase());

    return normalized.find((candidate) => candidate !== "null") ?? normalized[0] ?? null;
  }

  return null;
}

function getRequiredFields(schema: Record<string, unknown>) {
  const required = schema.required;

  if (!Array.isArray(required)) {
    return new Set<string>();
  }

  return new Set(
    required
      .filter((field): field is string => typeof field === "string")
      .map((field) => field.trim())
      .filter((field) => field !== "")
  );
}

function isUnsupportedOperationRule(rule: OperationRule | null) {
  const type = normalizeRuleType(rule);

  return !rule || type === "array" || type === "object";
}

export function getOperationFields(
  operations: Record<string, Record<string, unknown>> | null,
  operation: string
): OperationField[] {
  const operationSchema = getOperationSchema(operations, operation);

  if (!operationSchema) {
    return [];
  }

  const fields: OperationField[] = [];

  const pushLeafField = (path: string, required: boolean) => {
    const rule = path
      ? getOperationRuleForPath(operations, operation, path)
      : getOperationRule(operations, operation);

    fields.push({
      path,
      label: path || "value",
      required,
      rule,
      unsupported: isUnsupportedOperationRule(rule),
    });
  };

  const collect = (
    schema: Record<string, unknown>,
    currentPath: string,
    currentRequired: boolean
  ) => {
    const schemaType = getSchemaPrimaryType(schema);
    const properties = isPlainObject(schema.properties)
      ? (schema.properties as Record<string, unknown>)
      : null;

    if ((schemaType === "object" || schemaType === null) && properties) {
      const requiredFields = getRequiredFields(schema);
      const propertyEntries = Object.entries(properties).filter(([, propertySchema]) =>
        isPlainObject(propertySchema)
      );

      if (propertyEntries.length === 0) {
        pushLeafField(currentPath, currentRequired);
        return;
      }

      propertyEntries.forEach(([propertyName, propertySchema]) => {
        const normalizedPropertyName = propertyName.trim();
        if (!normalizedPropertyName) {
          return;
        }

        const propertyPath = currentPath
          ? `${currentPath}.${normalizedPropertyName}`
          : normalizedPropertyName;
        const propertyRequired = currentRequired && requiredFields.has(normalizedPropertyName);

        collect(propertySchema as Record<string, unknown>, propertyPath, propertyRequired);
      });

      return;
    }

    pushLeafField(currentPath, currentRequired);
  };

  collect(operationSchema as Record<string, unknown>, "", true);

  return fields;
}

function getOperationFieldFallbackRawValue(
  field: OperationField,
  fallbackValue: unknown
) {
  if (fallbackValue === null || fallbackValue === undefined) {
    return null;
  }

  const normalizedPath = field.path.trim();
  let value = normalizedPath
    ? getValueByPath(fallbackValue, normalizedPath)
    : fallbackValue;

  if (value === undefined && normalizedPath.startsWith("value.")) {
    value = getValueByPath({ value: fallbackValue }, normalizedPath);
  }

  if (value === undefined || value === null) {
    return null;
  }

  const fieldType = normalizeRuleType(field.rule);

  if (fieldType === "number" || fieldType === "integer") {
    if (typeof value === "number" && Number.isFinite(value)) {
      return String(value);
    }

    if (typeof value === "string") {
      const parsed = Number(value.trim());
      if (Number.isFinite(parsed)) {
        return String(parsed);
      }
    }

    return null;
  }

  if (fieldType === "boolean" && typeof value === "boolean") {
    return String(value);
  }

  if (typeof value === "string") {
    return value;
  }

  if (Array.isArray(field.rule?.enumValues) && field.rule.enumValues.includes(value)) {
    return String(value);
  }

  return null;
}

export function buildOperationFieldStates(
  fields: OperationField[],
  fieldValues: Record<string, string>,
  fallbackValue?: unknown
): OperationFieldState[] {
  return fields.map((field) => {
    const defaultFieldValue = field.required
      ? defaultValueForRule(field.rule)
      : "";
    const fallbackFieldValue = getOperationFieldFallbackRawValue(
      field,
      fallbackValue
    );
    const rawValue =
      fieldValues[field.path] ?? fallbackFieldValue ?? defaultFieldValue;
    const validation =
      rawValue.trim() === "" && !field.required
        ? {
          value: null,
          errorKey: null,
          errorOptions: undefined,
        }
        : validateCommandValue(field.rule, rawValue);

    return {
      ...field,
      rawValue,
      validation,
    };
  });
}

export function hasUnsupportedOperationFields(fields: OperationFieldState[]) {
  return fields.some((field) => field.unsupported);
}

export function hasOperationFieldValidationError(fields: OperationFieldState[]) {
  return fields.some((field) => field.validation.errorKey);
}

export function buildOperationPayload(fields: OperationFieldState[]) {
  const rootField = fields.find((field) => field.path === "");
  if (rootField) {
    return rootField.validation.value;
  }

  return fields.reduce<Record<string, unknown>>((currentPayload, field) => {
    if (field.unsupported) {
      return currentPayload;
    }

    if (field.rawValue.trim() === "" && !field.required) {
      return currentPayload;
    }

    if (field.path === "") {
      return currentPayload;
    }

    const nextPayload = composeValueByPath(field.path, field.validation.value);

    if (!isPlainObject(nextPayload)) {
      return currentPayload;
    }

    return mergeRecords(currentPayload, nextPayload);
  }, {});
}
