export type SchemaFieldType =
  | "boolean"
  | "number"
  | "integer"
  | "string"
  | "enum"
  | "unsupported";

export type SchemaField = {
  path: string;
  required: boolean;
  readOnly: boolean;
  type: SchemaFieldType;
  enumValues: string[];
  min: number | null;
  max: number | null;
  step: number | null;
};

export function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
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

  const variants = [schema.oneOf, schema.anyOf]
    .filter((candidate): candidate is unknown[] => Array.isArray(candidate))
    .flatMap((candidate) => candidate)
    .filter((candidate): candidate is Record<string, unknown> => isPlainObject(candidate))
    .map((candidate) => candidate.type)
    .flatMap((candidateType) => {
      if (typeof candidateType === "string") {
        return [candidateType.trim().toLowerCase()];
      }

      if (Array.isArray(candidateType)) {
        return candidateType
          .filter((item): item is string => typeof item === "string")
          .map((item) => item.trim().toLowerCase())
          .filter((item) => item !== "");
      }

      return [];
    });

  return variants.find((candidate) => candidate !== "null") ?? variants[0] ?? null;
}

function getSchemaVariantRecords(schema: Record<string, unknown>) {
  return [schema.oneOf, schema.anyOf]
    .filter((candidate): candidate is unknown[] => Array.isArray(candidate))
    .flatMap((candidate) => candidate)
    .filter((candidate): candidate is Record<string, unknown> => isPlainObject(candidate));
}

function getNumericConstraint(
  schema: Record<string, unknown>,
  key: "minimum" | "maximum" | "multipleOf" | "step",
  requirePositive = false
) {
  const candidates = [schema, ...getSchemaVariantRecords(schema)];

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

function getNumericStep(schema: Record<string, unknown>) {
  const step = getNumericConstraint(schema, "multipleOf", true);
  if (typeof step === "number") {
    return step;
  }

  return getNumericConstraint(schema, "step", true);
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

function getBooleanConstraint(
  schema: Record<string, unknown>,
  key: "readOnly"
) {
  const candidates = [schema, ...getSchemaVariantRecords(schema)];

  for (const candidate of candidates) {
    const value = candidate[key];

    if (typeof value === "boolean") {
      return value;
    }
  }

  return false;
}

export function toSchemaFields(schema: Record<string, unknown>): SchemaField[] {
  const fields: SchemaField[] = [];

  const pushLeafField = (
    path: string,
    required: boolean,
    fieldSchema: Record<string, unknown>
  ) => {
    const primaryType = getSchemaPrimaryType(fieldSchema);
    const enumValues = Array.isArray(fieldSchema.enum)
      ? fieldSchema.enum.map((value) => String(value))
      : [];

    const type: SchemaFieldType = enumValues.length > 0
      ? "enum"
      : primaryType === "boolean"
        ? "boolean"
        : primaryType === "number"
          ? "number"
          : primaryType === "integer"
            ? "integer"
            : primaryType === "string"
              ? "string"
              : "unsupported";

    fields.push({
      path,
      required,
      readOnly: getBooleanConstraint(fieldSchema, "readOnly"),
      type,
      enumValues,
      min: getNumericConstraint(fieldSchema, "minimum"),
      max: getNumericConstraint(fieldSchema, "maximum"),
      step: getNumericStep(fieldSchema),
    });
  };

  const collect = (
    currentSchema: Record<string, unknown>,
    currentPath: string,
    required: boolean
  ) => {
    const schemaType = getSchemaPrimaryType(currentSchema);
    const properties = isPlainObject(currentSchema.properties)
      ? (currentSchema.properties as Record<string, unknown>)
      : null;

    if ((schemaType === "object" || schemaType === null) && properties) {
      const requiredFields = getRequiredFields(currentSchema);
      const entries = Object.entries(properties).filter(([, fieldSchema]) =>
        isPlainObject(fieldSchema)
      );

      if (entries.length === 0) {
        pushLeafField(currentPath, required, currentSchema);
        return;
      }

      entries.forEach(([fieldName, fieldSchema]) => {
        const normalizedName = fieldName.trim();
        if (!normalizedName) {
          return;
        }

        const fieldPath = currentPath
          ? `${currentPath}.${normalizedName}`
          : normalizedName;

        collect(
          fieldSchema as Record<string, unknown>,
          fieldPath,
          required && requiredFields.has(normalizedName)
        );
      });
      return;
    }

    pushLeafField(currentPath, required, currentSchema);
  };

  collect(schema, "", true);

  return fields;
}
