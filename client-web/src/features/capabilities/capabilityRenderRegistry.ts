import {
  formatRgbValue,
  getRgbHex,
  type RgbChannel,
} from "./rgbCapabilityUtils";

type FieldLike = {
  path: string;
  type?: string | null;
  enumValues?: unknown[];
  min?: number | null;
  max?: number | null;
  unsupported?: boolean;
};

export type FieldRenderKind =
  | "boolean"
  | "color"
  | "enum"
  | "numeric-slider"
  | "number"
  | "text"
  | "unsupported";

const RGB_CAPABILITY_ID = "light.rgb";
export const LOCK_STATE_CAPABILITY_ID = "lock.state";
export const LIGHT_SENSOR_CAPABILITY_ID = "sensor.light";
export const MOTION_SENSOR_CAPABILITY_ID = "sensor.motion";
export const RGB_CHANNELS: RgbChannel[] = ["red", "green", "blue"];

type BooleanValueLabels = {
  trueLabel: string;
  falseLabel: string;
};

export type CapabilityBooleanLabels = {
  on: string;
  off: string;
  locked?: string;
  unlocked?: string;
  byCapability?: Record<string, BooleanValueLabels>;
};

export type CapabilityStateRenderPlan =
  | {
    kind: "rgb";
    swatchHex: string;
    title: string;
  }
  | {
    kind: "lock";
    locked: boolean;
    text: string;
  }
  | {
    kind: "text";
    text: string;
  };

export type CapabilityFieldEditorRenderPlan<TField extends FieldLike> =
  | {
    kind: "rgb";
    channelPaths: Record<RgbChannel, string>;
    skippedFields: TField[];
  }
  | {
    kind: "lock";
    field: TField;
    skippedFields: TField[];
  }
  | {
    kind: "schema";
    fields: TField[];
    skippedFields: TField[];
  }
  | {
    kind: "unsupported";
    skippedFields: TField[];
  };

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function getScalarStateValue(value: unknown) {
  if (!isRecord(value)) {
    return value;
  }

  if ("value" in value) {
    return value.value;
  }

  const values = Object.values(value);
  return values.length === 1 ? values[0] : value;
}

function formatNumberStateValue(value: number, precision: number | null | undefined) {
  if (!Number.isFinite(value)) {
    return String(value);
  }

  if (typeof precision !== "number" || !Number.isInteger(precision)) {
    return String(value);
  }

  const boundedPrecision = Math.max(0, Math.min(precision, 6));
  return Number(value.toFixed(boundedPrecision)).toString();
}

export function normalizeCapabilityId(capabilityId: string | null | undefined) {
  return capabilityId?.trim().toLowerCase() ?? "";
}

export function isRgbCapabilityId(capabilityId: string | null | undefined) {
  return normalizeCapabilityId(capabilityId) === RGB_CAPABILITY_ID;
}

export function isLockStateCapabilityId(capabilityId: string | null | undefined) {
  return normalizeCapabilityId(capabilityId) === LOCK_STATE_CAPABILITY_ID;
}

export function getCapabilityBooleanLabel(
  capabilityId: string | null | undefined,
  value: boolean,
  labels: CapabilityBooleanLabels
) {
  const normalizedCapabilityId = normalizeCapabilityId(capabilityId);
  const capabilityLabels = labels.byCapability?.[normalizedCapabilityId];

  if (capabilityLabels) {
    return value ? capabilityLabels.trueLabel : capabilityLabels.falseLabel;
  }

  if (normalizedCapabilityId === LOCK_STATE_CAPABILITY_ID) {
    return value ? (labels.locked ?? labels.on) : (labels.unlocked ?? labels.off);
  }

  return value ? labels.on : labels.off;
}

export function resolveCapabilityStateRender(
  capabilityId: string | null | undefined,
  value: unknown,
  options: {
    fallbackText?: string | null;
    rgbLabels?: Partial<Record<RgbChannel, string>>;
    booleanLabels?: CapabilityBooleanLabels;
    numberPrecision?: number | null;
  } = {}
): CapabilityStateRenderPlan | null {
  const capabilitySpecificRender = resolveCapabilitySpecificStateRender(
    capabilityId,
    value,
    options
  );

  if (capabilitySpecificRender) {
    return capabilitySpecificRender;
  }

  const scalarValue = getScalarStateValue(value);

  if (typeof scalarValue === "boolean" && options.booleanLabels) {
    return {
      kind: "text",
      text: getCapabilityBooleanLabel(capabilityId, scalarValue, options.booleanLabels),
    };
  }

  if (typeof scalarValue === "string") {
    return {
      kind: "text",
      text: scalarValue,
    };
  }

  if (typeof scalarValue === "number") {
    return {
      kind: "text",
      text: formatNumberStateValue(scalarValue, options.numberPrecision),
    };
  }

  const fallbackText = options.fallbackText?.trim();
  if (fallbackText) {
    return {
      kind: "text",
      text: fallbackText,
    };
  }

  return null;
}

function resolveCapabilitySpecificStateRender(
  capabilityId: string | null | undefined,
  value: unknown,
  options: {
    rgbLabels?: Partial<Record<RgbChannel, string>>;
    booleanLabels?: CapabilityBooleanLabels;
  }
): CapabilityStateRenderPlan | null {
  if (isRgbCapabilityId(capabilityId)) {
    const rgbHex = getRgbHex(value);
    const rgbText = formatRgbValue(value, options.rgbLabels);

    if (rgbHex) {
      return {
        kind: "rgb",
        swatchHex: rgbHex,
        title: rgbText ?? rgbHex,
      };
    }

    if (rgbText) {
      return {
        kind: "text",
        text: rgbText,
      };
    }

    return null;
  }

  if (isLockStateCapabilityId(capabilityId) && options.booleanLabels) {
    const scalarValue = getScalarStateValue(value);

    if (typeof scalarValue === "boolean") {
      return {
        kind: "lock",
        locked: scalarValue,
        text: getCapabilityBooleanLabel(capabilityId, scalarValue, options.booleanLabels),
      };
    }
  }

  return null;
}

export function getSchemaFieldRenderKind(field: FieldLike): FieldRenderKind {
  if (field.unsupported) {
    return "unsupported";
  }

  const type = field.type?.trim().toLowerCase() ?? null;
  const enumValues = field.enumValues ?? [];
  const normalizedPath = field.path.trim().toLowerCase();
  const pathSegments = normalizedPath.split(".").filter(Boolean);
  const finalPathSegment = pathSegments[pathSegments.length - 1] ?? normalizedPath;

  if (Array.isArray(enumValues) && enumValues.length > 0) {
    return "enum";
  }

  if (type === "boolean") {
    return "boolean";
  }

  if (type === "integer" || type === "number") {
    return typeof field.min === "number" &&
      typeof field.max === "number" &&
      field.min <= field.max
      ? "numeric-slider"
      : "number";
  }

  if (type === "string") {
    if (finalPathSegment === "color") {
      return "color";
    }

    return "text";
  }

  return "unsupported";
}

function isSchemaFieldRenderable(field: FieldLike) {
  return getSchemaFieldRenderKind(field) !== "unsupported";
}

function supportsSetOperationEditor(operation: string | null | undefined) {
  const normalizedOperation = operation?.trim().toLowerCase();
  return !normalizedOperation || normalizedOperation === "set";
}

function getSingleFieldByPath<TField extends FieldLike>(
  fields: TField[],
  path: string
) {
  const normalizedPath = path.trim().toLowerCase();
  const matchedFields = fields.filter(
    (field) => field.path.trim().toLowerCase() === normalizedPath
  );

  if (matchedFields.length !== 1 || fields.length !== 1) {
    return null;
  }

  return matchedFields[0];
}

function getExactRgbChannelPaths(
  capabilityId: string | null | undefined,
  fields: FieldLike[]
) {
  const channelPaths = getRgbChannelPaths(capabilityId, fields);

  if (!channelPaths) {
    return null;
  }

  const channelPathSet = new Set(
    Object.values(channelPaths).map((path) => path.trim().toLowerCase())
  );
  const hasOnlyRgbFields =
    fields.length === RGB_CHANNELS.length &&
    fields.every((field) => channelPathSet.has(field.path.trim().toLowerCase()));

  return hasOnlyRgbFields ? channelPaths : null;
}

function resolveCapabilitySpecificFieldEditorRender<TField extends FieldLike>(
  capabilityId: string | null | undefined,
  fields: TField[],
  operation?: string | null
): CapabilityFieldEditorRenderPlan<TField> | null {
  if (!supportsSetOperationEditor(operation)) {
    return null;
  }

  if (isRgbCapabilityId(capabilityId)) {
    const channelPaths = getExactRgbChannelPaths(capabilityId, fields);

    if (!channelPaths) {
      return null;
    }

    return {
      kind: "rgb",
      channelPaths,
      skippedFields: [],
    };
  }

  if (isLockStateCapabilityId(capabilityId)) {
    const lockedField = getSingleFieldByPath(fields, "locked");

    if (!lockedField || getSchemaFieldRenderKind(lockedField) !== "boolean") {
      return null;
    }

    return {
      kind: "lock",
      field: lockedField,
      skippedFields: [],
    };
  }

  return null;
}

export function resolveCapabilityFieldEditorRender<TField extends FieldLike>(
  capabilityId: string | null | undefined,
  fields: TField[],
  operation?: string | null
): CapabilityFieldEditorRenderPlan<TField> {
  const capabilitySpecificRender = resolveCapabilitySpecificFieldEditorRender(
    capabilityId,
    fields,
    operation
  );

  if (capabilitySpecificRender) {
    return capabilitySpecificRender;
  }

  const renderableFields = fields.filter(isSchemaFieldRenderable);
  const skippedFields = fields.filter((field) => !isSchemaFieldRenderable(field));

  if (renderableFields.length > 0 || fields.length === 0) {
    return {
      kind: "schema",
      fields: renderableFields,
      skippedFields,
    };
  }

  return {
    kind: "unsupported",
    skippedFields,
  };
}

function getRgbChannelPathCandidates(channel: RgbChannel) {
  return [`value.${channel}`, channel, `color.${channel}`];
}

function getRgbChannelPaths(
  capabilityId: string | null | undefined,
  fields: Array<{ path: string; }>
): Record<RgbChannel, string> | null {
  if (!isRgbCapabilityId(capabilityId)) {
    return null;
  }

  const fieldPathByNormalized = new Map(
    fields.map((field) => {
      const path = field.path.trim();
      return [path.toLowerCase(), path];
    })
  );
  const channelEntries: Array<[RgbChannel, string] | null> = RGB_CHANNELS.map((channel) => {
    const path = getRgbChannelPathCandidates(channel)
      .map((candidate) => fieldPathByNormalized.get(candidate.toLowerCase()))
      .find((candidate): candidate is string => typeof candidate === "string");

    return path ? [channel, path] : null;
  });

  if (channelEntries.some((entry) => entry === null)) {
    return null;
  }

  return Object.fromEntries(
    channelEntries.filter((entry): entry is [RgbChannel, string] => entry !== null)
  ) as Record<RgbChannel, string>;
}

export function localizeSchemaPath(
  path: string,
  rootLabel: string,
  localizeSegment: (segment: string) => string
) {
  const segments = path
    .split(".")
    .map((segment) => segment.trim())
    .filter((segment) => segment !== "");

  if (segments.length === 0) {
    return rootLabel;
  }

  return segments.map(localizeSegment).join(".");
}
